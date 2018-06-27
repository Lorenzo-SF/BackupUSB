using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using WPF = System.Windows.Controls;
using FORMS = System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Configuration;

namespace BackupUSB
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Origin;
        public string Destination;
        public List<string> Extensions = new List<string>();

        public string PATH_BACKUP_PRINCIPAL = ConfigurationManager.AppSettings["PathPrimary"];
        public string PATH_BACKUP_SECONDARY = ConfigurationManager.AppSettings["PathSecondary"];

        public MainWindow()
        {
            InitializeComponent();
            LoadCombos();
            Extensions.Add(".*");
            boxEstado.Header = Constants.LABEL_TYPE_STATUS;
        }
        
        #region Cargas
        /// <summary>
        /// Funcion que carga el combo "Destino" con las unidades disponibles.
        /// </summary>
        private void LoadCombos()
        {
            cmbDrives.ItemsSource = DriveInfo.GetDrives().Where(x => x.IsReady);
            cmbDrives.DisplayMemberPath = "RootDirectory";

            var typeBackups = new List<string>
            {
                Constants.TYPE_BACKUP_REPLACE_ORIGIN_DESTINATION,
                Constants.TYPE_BACKUP_COPY_ORIGIN_NOT_IN_DESTINATION,
                Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES,
                Constants.TYPE_BACKUP_COMPARE_HASH
            };

            cmbTipoBackup.ItemsSource = typeBackups;
        }
        #endregion

        #region Procesado

        /// <summary>
        /// Función ejecutada por el boton "Hacer Backup". Prepara el "BackgroundWorker" para lanzarlo (tiene que ser asincrono para que no se pete).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessBackup(object sender, RoutedEventArgs e)
        {
            var parseResult = Functions.ParsePath(tbxOrigin, boxOrigin, boxDestination, boxEstado, boxTipoBackup, cmbDrives, cmbTipoBackup, cmbTipoCopy, cmbCantidadArchivos, lblFile, imgEstado, ref Destination);
            Origin = tbxOrigin.Text;

            if (parseResult)
            {
                BackgroundWorker bgwkBackup = new BackgroundWorker();
                bgwkBackup.WorkerReportsProgress = true;
                bgwkBackup.DoWork += MakeBackup;
                bgwkBackup.ProgressChanged += ProgressChanged;

                bgwkBackup.RunWorkerAsync();                
            }
        }

        /// <summary>
        /// Función para el "BackgroundWorker". Crea el directorio de destino, la estructura de carpetas y copia los archivos en su destino. Tambien muestra la ruta de destino y el archivo que se está procesando actualmente.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MakeBackup(object sender, DoWorkEventArgs e)
        {
            var file = string.Empty;
            var typeBackupSelected = string.Empty;
            var typeCopySelected = string.Empty;
            var typeQuantityFilesSelected = string.Empty;
            var isOverwrite = false;
            var isAllFiles = false;


            DisableControls();

            cmbTipoBackup.Dispatcher.Invoke(() =>
            {
                typeBackupSelected = cmbTipoBackup.SelectedValue.ToString();
            });

            if (typeBackupSelected.Equals(Constants.TYPE_BACKUP_COMPARE_HASH) ||
                typeBackupSelected.Equals(Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES)) {

                cmbTipoCopy.Dispatcher.Invoke(() =>
                {
                    typeCopySelected = cmbTipoCopy.SelectedValue.ToString();
                });

                cmbCantidadArchivos.Dispatcher.Invoke(() =>
                {
                    typeQuantityFilesSelected = cmbCantidadArchivos.SelectedValue.ToString();
                });

                isOverwrite = typeCopySelected.Equals(Constants.TYPE_COPY_OVERWRITE);
                isAllFiles = typeQuantityFilesSelected.Equals(Constants.TYPE_COPY_ALL_FILLES);
            }

            try
            {
                switch (typeBackupSelected)
                {
                    case Constants.TYPE_BACKUP_REPLACE_ORIGIN_DESTINATION:
                        file = Functions.ReplaceDestinationByOrigin(sender, Origin, Destination, Extensions, lblFile);
                        break;

                    case Constants.TYPE_BACKUP_COPY_ORIGIN_NOT_IN_DESTINATION:
                        file = Functions.CopyOriginNotInDestination(sender, Origin, Destination, Extensions, lblFile);
                        break;

                    case Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES:
                        file = Functions.CopyComparingAttributes(sender, Origin, Destination, Extensions, lblFile, isOverwrite, isAllFiles);
                        break;

                    case Constants.TYPE_BACKUP_COMPARE_HASH:
                        file = Functions.CopyComparingHash(sender, Origin, Destination, Extensions, lblFile, isOverwrite, isAllFiles);
                        break;
                }
            }
            catch (Exception ex)
            {
                lblFile.Dispatcher.Invoke(() =>
                {
                    lblFile.Content = "Hubo un error al copiar. " + file;
                });
            }

            EnableControls();
        }

        /// <summary>
        /// Función para actualizar el "ProgressBar" con el porcentaje recibido. 
        /// Se envia desde el metodo establecido en la propiedad "DoWork" del "BackgroundWorker" a traves de "(sender as BackgroundWorker).ReportProgress(int porcentaje);"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbPorcentajeCopia.Value = e.ProgressPercentage;
        }

        #endregion

        #region Listeners

        /// <summary>
        /// Listener de los botones de atajos. Meten en la caja de texto "origen" paths preestablecidos.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shortcut(object sender, RoutedEventArgs e)
        {
            var button = (WPF.Button)sender;
            var name = button.Name;
            var path = string.Empty;

            switch (name)
            {
                case Constants.BUTTON_SHORTCUT_BACKUP_PRINCIPAL_FILES:
                    path = PATH_BACKUP_PRINCIPAL;
                    break;
                case Constants.BUTTON_SHORTCUT_BACKUP_AUTOREPLACEMENTS:
                    path = PATH_BACKUP_SECONDARY;
                    break;
            }

            tbxOrigin.Text = path;

        }

        /// <summary>
        /// Listener para el boton "Examinar". Saca el dialog para seleccionar directorios y mete en la caja de texto "origen" el path seleccionado.
        /// Tambien rellena el combo de "Extensiones
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowsePath(object sender, RoutedEventArgs e)
        {
            var button = (WPF.Button)sender;
            var name = button.Name;
            
            using (var fbd = new FORMS.FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();

                if (result == FORMS.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    switch (name)
                    {
                        case Constants.BUTTON_BROWSE_ORIGIN_PATH:
                            tbxOrigin.Text = fbd.SelectedPath;
                            
                            cmbExtensiones.ItemsSource = Functions.LoadExtensionsCombo(new DirectoryInfo(fbd.SelectedPath)); 
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Listner para el combo de "Unidades". Coge la unidad seleccionada y muestra el tamaño total y el disponible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectComboList(object sender, WPF.SelectionChangedEventArgs e)
        {
            var combo = (WPF.ComboBox)sender;
            var name = combo.Name;
            var drive = combo.SelectedValue.ToString();
            
            switch (name)
            {
                case Constants.COMBO_DRIVES_LIST:
                    foreach (var d in DriveInfo.GetDrives())
                    {
                        if (d.IsReady && d.RootDirectory.ToString().Equals(drive))
                        {
                            lblDrives.Content = string.Format("Name drive: {0}\nTotal size: {1}\nAvailable space: {2}", d.VolumeLabel, Functions.ConvertBytes(d.TotalSize), Functions.ConvertBytes(d.AvailableFreeSpace)); ;
                        }
                    }
                    break;
                case Constants.COMBO_TYPE_BACKUP:
                    if (combo.SelectedValue.ToString().Equals(Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES) ||
                        combo.SelectedValue.ToString().Equals(Constants.TYPE_BACKUP_COMPARE_HASH)){

                        var typeCopys = new List<string>
                        {
                            Constants.TYPE_COPY_OVERWRITE,
                            Constants.TYPE_COPY_ADD_NEW_FILE
                        };

                        var typeQuantityFiles = new List<string>
                        {
                            Constants.TYPE_COPY_ALL_FILLES,
                            Constants.TYPE_COPY_ONLY_COMMON_FILES
                        };

                        cmbTipoCopy.ItemsSource = typeCopys;
                        cmbTipoCopy.IsEnabled = true;

                        cmbCantidadArchivos.ItemsSource = typeQuantityFiles;
                        cmbCantidadArchivos.IsEnabled = true;
                    }
                    else {
                        cmbTipoCopy.ItemsSource = null;
                        cmbTipoCopy.IsEnabled = false;

                        cmbCantidadArchivos.ItemsSource = null;
                        cmbCantidadArchivos.IsEnabled = false;
                    }
                    break;
            }            
        }

        /// <summary>
        /// Listener para el combo de "Extensiones. Mantiene actualizada la lista de extensiones a filtrar. Si no se selecciona nada, se pone "*.*" para que use todo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckExtension(object sender, RoutedEventArgs  e) {
            var chbx = (WPF.CheckBox)sender;
            var isChecked = chbx.IsChecked != null && (bool)chbx.IsChecked;
            var content = chbx.Content.ToString();
            
            if (isChecked && !Extensions.Contains(content))
            {
                Extensions.Add(content);
            }
            else if(!isChecked && Extensions.Contains(content))
            {
                Extensions.Remove(content);
            }

            if (!Extensions.Any()){
                Extensions.Add(Constants.PATTERN_ALL_FILES);
            }
            else {
                Extensions.Remove(Constants.PATTERN_ALL_FILES);
            }

            lblExtensiones.Content = Extensions.Contains(Constants.PATTERN_ALL_FILES) ? Constants.ALL_EXTENSIONS_SELECTED : string.Format(Constants.SOME_EXTENSIONS_SELECTED, Extensions.Count());
        }

        /// <summary>
        /// Funcion para cargar el combo de extensiones si se mete el path a mano (sin el dialog)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OriginTextChanged(object sender, WPF.TextChangedEventArgs e)
        {
            var txb = (WPF.TextBox)sender;
            var text = txb.Text;

            if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
            {
                cmbExtensiones.IsEnabled = false;
                lblExtensiones.Content = string.Empty;
            }
            else
            {
                cmbExtensiones.IsEnabled = true;
                lblExtensiones.Content = Constants.ALL_EXTENSIONS_SELECTED;
                cmbExtensiones.ItemsSource = Functions.LoadExtensionsCombo(new DirectoryInfo(text));
            }
        }
        #endregion

        #region Funciones de controles
        /// <summary>
        /// Funcion que deshabilita los controles mientras se hace la copia
        /// </summary>
        private void DisableControls() {
            boxEstado.Dispatcher.Invoke(() =>
            {
                boxEstado.Header = Constants.LABEL_TYPE_STATUS_PROCESING;
            });

            imgEstado.Dispatcher.Invoke(() =>
            {
                imgEstado.Source = new BitmapImage(new Uri(@"/Images/running.gif", UriKind.Relative));
                
            });

            tbxOrigin.Dispatcher.Invoke(() =>
            {
                tbxOrigin.IsEnabled = false;
            });

            cmbDrives.Dispatcher.Invoke(() =>
            {
                cmbDrives.IsEnabled = false;
            });

            cmbExtensiones.Dispatcher.Invoke(() =>
            {
                cmbExtensiones.IsEnabled = false;
            });

            cmbTipoBackup.Dispatcher.Invoke(() =>
            {
                cmbTipoBackup.IsEnabled = false;
            });

            cmbTipoCopy.Dispatcher.Invoke(() =>
            {
                cmbTipoCopy.IsEnabled = false;
            });

            btnBackup.Dispatcher.Invoke(() =>
            {
                btnBackup.IsEnabled = false;
            });

            btnBrowseOriginPath.Dispatcher.Invoke(() =>
            {
                btnBrowseOriginPath.IsEnabled = false;
            });

            btnPathSecondary.Dispatcher.Invoke(() =>
            {
                btnPathSecondary.IsEnabled = false;
            });

            btnPathPrimary.Dispatcher.Invoke(() =>
            {
                btnPathPrimary.IsEnabled = false;
            });
        }

        /// <summary>
        /// Funcion que habilita los controles al terminar la copia.
        /// </summary>
        private void EnableControls()
        {
            boxEstado.Dispatcher.Invoke(() =>
            {
                boxEstado.Header = Constants.LABEL_TYPE_STATUS_OK;
            });
            
            imgEstado.Dispatcher.Invoke(() =>
            {
                imgEstado.Source = new BitmapImage(new Uri(@"/Images/success.jpg", UriKind.Relative));
            });

            tbxOrigin.Dispatcher.Invoke(() =>
            {
                tbxOrigin.IsEnabled = true;
            });

            cmbDrives.Dispatcher.Invoke(() =>
            {
                cmbDrives.IsEnabled = true;
            });

            cmbExtensiones.Dispatcher.Invoke(() =>
            {
                cmbExtensiones.IsEnabled = true;
            });

            cmbTipoBackup.Dispatcher.Invoke(() =>
            {
                cmbTipoBackup.IsEnabled = true;

                if (!string.IsNullOrWhiteSpace(cmbTipoBackup.SelectedValue.ToString()) &&
                    (cmbTipoBackup.SelectedValue.ToString().Equals(Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES) ||
                    cmbTipoBackup.SelectedValue.ToString().Equals(Constants.TYPE_BACKUP_COMPARE_HASH)))
                {
                    cmbTipoCopy.Dispatcher.Invoke(() =>
                    {
                        cmbTipoCopy.IsEnabled = true;
                    });
                }
                else
                {
                    cmbTipoCopy.Dispatcher.Invoke(() =>
                    {
                        cmbTipoCopy.IsEnabled = false;
                    });
                }
            });

            btnBackup.Dispatcher.Invoke(() =>
            {
                btnBackup.IsEnabled = true;
            });

            btnBrowseOriginPath.Dispatcher.Invoke(() =>
            {
                btnBrowseOriginPath.IsEnabled = true;
            });

            btnPathSecondary.Dispatcher.Invoke(() =>
            {
                btnPathSecondary.IsEnabled = true;
            });

            btnPathPrimary.Dispatcher.Invoke(() =>
            {
                btnPathPrimary.IsEnabled = true;
            });
        }
        #endregion
        
    }
}
