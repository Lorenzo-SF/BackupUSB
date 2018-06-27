using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BackupUSB
{
    public static class Functions
    {
        /// <summary>
        /// Función que revisa: La ruta está vacía. La ruta no existe. La unidad no seleccionada. Se
        /// ha seleccionado unidad pero la ruta está vacia. La ruta de destino ya existe.
        /// </summary>
        /// <returns></returns>
        public static bool ParsePath(TextBox tbxOrigin, GroupBox boxOrigin, GroupBox boxDestination, GroupBox boxEstado, GroupBox boxTipoBackup,
                                     ComboBox cmbDrives, ComboBox cmbTipoBackup, ComboBox cmbTipoCopy, ComboBox cmbCantidadArchivos,
                                     Label lblFile, Image imgEstado, ref string destination)
        {
            var result = true;
            var pathSplit = tbxOrigin.Text.Split('\\').Where(x => !string.IsNullOrWhiteSpace(x.ToString()));
            var rootFolder = string.Empty;

            if (pathSplit.Any())
            {
                rootFolder = pathSplit.Last();
            }

            // la ruta está vacía
            if (string.IsNullOrWhiteSpace(tbxOrigin.Text))
            {
                boxOrigin.Header = Constants.LABEL_ORIGIN_EMPTY;
                result = false;
            }

            // la ruta no existe
            if (!string.IsNullOrWhiteSpace(tbxOrigin.Text) && !Directory.Exists(tbxOrigin.Text))
            {
                boxOrigin.Header = Constants.LABEL_ORIGIN_NOT_EXISTS;
                result = false;
            }

            // unidad no seleccionada
            if (cmbDrives.SelectedIndex == -1)
            {
                boxDestination.Header = Constants.LABEL_DESTINATION_EMPTY;
                result = false;
            }

            // se ha seleccionado unidad pero la ruta está vacia
            if (cmbDrives.SelectedIndex != -1 && (string.IsNullOrWhiteSpace(tbxOrigin.Text) || !Directory.Exists(tbxOrigin.Text)))
            {
                boxDestination.Header = Constants.LABEL_DESTINATION_NOT_VALID;
                result = false;
            }

            // si no hay tipo de backup seleccionado o se ha seleccionado "Constants.TYPE_BACKUP_COMPARE_HASH" o "Constants.TYPE_BACKUP_COMPARE_ATTRIBUTES" y no se ha especificado el tipo de copia
            if (cmbTipoBackup.SelectedIndex == -1 ||
                ((cmbTipoBackup.SelectedIndex == 2 || cmbTipoBackup.SelectedIndex == 3) && cmbTipoCopy.SelectedIndex == -1) ||
                ((cmbTipoBackup.SelectedIndex == 2 || cmbTipoBackup.SelectedIndex == 3) && cmbTipoCopy.SelectedIndex != -1 && cmbCantidadArchivos.SelectedIndex == -1))
            {
                if (cmbTipoBackup.SelectedIndex == -1)
                {
                    boxTipoBackup.Header = Constants.LABEL_TYPE_BACKUP_NOT_SELECTED;
                }
                else if ((cmbTipoBackup.SelectedIndex == 2 || cmbTipoBackup.SelectedIndex == 3) && cmbTipoCopy.SelectedIndex == -1)
                {
                    boxTipoBackup.Header = Constants.LABEL_TYPE_COPY_NOT_SELECTED;
                }
                else if ((cmbTipoBackup.SelectedIndex == 2 || cmbTipoBackup.SelectedIndex == 3) && cmbTipoCopy.SelectedIndex != -1 && cmbCantidadArchivos.SelectedIndex == -1)
                {
                    boxTipoBackup.Header = Constants.LABEL_QUANTITY_FILES_NOT_SELECTED;
                }
                result = false;
            }
            else
            {
                boxTipoBackup.Header = Constants.LABEL_TYPE_BACKUP;
            }

            // si todo ok
            if (!string.IsNullOrWhiteSpace(tbxOrigin.Text) && Directory.Exists(tbxOrigin.Text))
            {
                boxOrigin.Header = Constants.LABEL_ORIGIN_OK;
            }

            if (cmbDrives.SelectedIndex != -1)
            {
                boxDestination.Header = Constants.LABEL_DESTINATION_OK;
                destination = string.Format("{0}{1}{2}{3}", cmbDrives.SelectedValue.ToString(), Constants.BACKSLASH, rootFolder, Constants.BACKSLASH);
            }

            if (!result)
            {
                boxEstado.Header = Constants.LABEL_TYPE_STATUS_WITH_ERRORS;
                imgEstado.Source = new BitmapImage(new Uri(@"/Images/error.jpg", UriKind.Relative));
                lblFile.Content = string.Format("{0}\n{1}", boxOrigin.Header, boxDestination.Header);
            }

            return result;
        }

        /// <summary>
        /// Función que recibe un tamaño en Bytes y lo convierte a algo mas legible (KB, MB, GB).
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ConvertBytes(double size)
        {
            var realSize = size;
            var unity = "B";

            if (size < 1000000.0)
            {
                realSize = size / 1024f;
                unity = "KB";
            }

            if (size > 1000000.0 && size < 1000000000.0)
            {
                realSize = (size / 1024f) / 1024f;
                unity = "MB";
            }

            if (size > 1000000000.0)
            {
                realSize = ((size / 1024f) / 1024f) / 1024f;
                unity = "GB";
            }

            return Math.Round(realSize, 2) + " " + unity;
        }

        /// <summary>
        /// Funcion que devuelve la coleccion de extensiones del path recibido en "di" para cargar el combo de extensiones
        /// </summary>
        /// <param name="di"></param>
        /// <returns></returns>
        public static IEnumerable LoadExtensionsCombo(DirectoryInfo di)
        {
            return di.EnumerateFiles("*.*", SearchOption.AllDirectories)
                                                           .GroupBy(x => x.Extension)
                                                           .Select(g => new { Extension = g.Key })
                                                           .Where(g => !string.IsNullOrWhiteSpace(g.Extension))
                                                           .ToList();
        }

        /// <summary>
        /// Funcion que devuelve una lista de ficheros.
        /// Dependiendo de si en extensions vienen las extensiones pedidas o todas (solo tiene un elemento con valor "Constants.PATTERN_ALL_FILES")
        /// Devolvemos la lista de todos los archivos del origen o solo lo filtrado
        /// </summary>
        /// <param name="Extensions"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetFiles(List<string> Extensions, string origin, SearchOption searchOption)
        {
            if (Extensions.Contains(Constants.PATTERN_ALL_FILES))
            {
                return Directory.EnumerateFiles(origin, "*.*", searchOption);
            }

            return Directory.EnumerateFiles(origin, "*.*", searchOption)
                .Where(inS => Extensions.Contains(Path.GetExtension(inS), comparer: StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Funcion que devuelve una lista de directorios.
        /// Dependiendo de si en extensions vienen las extensiones pedidas o todas (solo tiene un elemento con valor "Constants.PATTERN_ALL_FILES")
        /// Devolvemos la lista de todos los directorios del origen o solo los directorios que tienen archivos afectados por el filtrado
        /// </summary>
        /// <param name="Extensions"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static List<string> GetDirectories(List<string> Extensions, string origin)
        {
            List<string> directoryList;

            if (Extensions.Contains(Constants.PATTERN_ALL_FILES))
            {
                directoryList = Directory.EnumerateDirectories(origin, "*", SearchOption.AllDirectories)
                                    .ToList();
            }
            else
            {
                directoryList = Directory.EnumerateFiles(origin, "*.*", SearchOption.AllDirectories)
                                    .Where(inS => Extensions.Contains(Path.GetExtension(inS), StringComparer.OrdinalIgnoreCase))
                                    .Select(Path.GetDirectoryName)
                                    .Distinct().ToList();
            }
            directoryList.Add(origin);
            return directoryList;
        }

        /// <summary>
        /// Funcion que devuelve la cantidad de ficheros a procesar en el origen.
        /// Dependiendo de si en extensions vienen las extensiones pedidas o todas (solo tiene un elemento con valor "Constants.PATTERN_ALL_FILES")
        /// Devolvemos la cantidad de archivos de todos los directorios del origen o solo la cantidad que hay en los directorios que tienen archivos afectados por el filtrado
        /// </summary>
        /// <param name="Extensions"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static int GetCountFiles(List<string> Extensions, string origin)
        {
            if (Extensions.Contains(Constants.PATTERN_ALL_FILES))
            {
                return Directory.EnumerateFiles(origin, "*.*", SearchOption.AllDirectories).Count();
            }

            return Directory.EnumerateFiles(origin, "*.*", SearchOption.AllDirectories)
                                .Count(inS => Extensions.Contains(Path.GetExtension(inS), StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Funcion para hacer la copia "TYPE_BACKUP_REPLACE_ORIGIN_DESTINATION"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="file"></param>
        /// <param name="time"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="extensions"></param>
        /// <param name="lblFile"></param>
        /// <param name="listFiles"></param>
        /// <param name="listDirectories"></param>
        /// <returns></returns>
        public static string ReplaceDestinationByOrigin(object sender, string origin, string destination, List<string> extensions, Label lblFile)
        {
            var file = string.Empty;
            int totalFiles;
            var time = Stopwatch.StartNew();
            IEnumerable<string> listFiles;
            IEnumerable<string> listDirectories;
            var processedFiles = 0;

            try
            {
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, true);
                }
                else
                {
                    Directory.CreateDirectory(destination);
                }

                listFiles = GetFiles(extensions, origin, SearchOption.AllDirectories);
                listDirectories = GetDirectories(extensions, origin);
                totalFiles = GetCountFiles(extensions, origin);

                foreach (string dirPath in listDirectories)
                {
                    Directory.CreateDirectory(dirPath.Replace(origin, destination));
                }

                foreach (string newPath in listFiles)
                {
                    file = Path.GetFileName(newPath);
                    var sizeFile = (new FileInfo(newPath)).Length;
                    // desde el metodo "ReportProgress" del "BackgroundWorker" enviamos el porcentaje a mostrar por el "ProgressBar".
                    // Se envia desde el metodo establecido en la propiedad "DoWork" del "BackgroundWorker" y se recibe por la propiedad "ProgressChanged" (tambien del "BackgroundWorker").
                    (sender as BackgroundWorker).ReportProgress((processedFiles * 100) / totalFiles);
                    processedFiles++;
                    lblFile.Dispatcher.Invoke(new Action(() =>
                    {
                        lblFile.Content = string.Format("Destination folder: {0}\tFiles to copy: {1}\nFiles copied: {2}\nSize: {3}\tCopying file: {4}", destination, totalFiles.ToString(), processedFiles.ToString(), ConvertBytes(sizeFile), file);
                    }));

                    File.Copy(newPath, newPath.Replace(origin, destination), true);
                }

                lblFile.Dispatcher.Invoke(() =>
                {
                    lblFile.Content = string.Format("Destination folder: {0}\tFiles copied: {1}\nTime elapsed: {2} Days {3} Hours {4} Minutes {5} Seconds {6} Milliseconds", destination, totalFiles.ToString(), time.Elapsed.Days.ToString(), time.Elapsed.Hours.ToString(), time.Elapsed.Minutes.ToString(), time.Elapsed.Seconds.ToString(), time.Elapsed.Milliseconds.ToString());
                });

                (sender as BackgroundWorker).ReportProgress(100);
            }
            catch (Exception ex)
            {
                return file;
            }
            return file;
        }

        /// <summary>
        /// Funcion para hacer la copia "TYPE_BACKUP_COPY_ORIGIN_NOT_IN_DESTINATION"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="extensions"></param>
        /// <param name="lblFile"></param>
        /// <returns></returns>
        public static string CopyOriginNotInDestination(object sender, string origin, string destination, List<string> extensions, Label lblFile)
        {
            var file = string.Empty;
            var totalFiles = 0;
            var processedFiles = 0;
            var time = Stopwatch.StartNew();
            IEnumerable<string> listFiles;
            IEnumerable<string> listDirectories;

            try
            {
                if (!Directory.Exists(destination))
                {
                    ReplaceDestinationByOrigin(sender, origin, destination, extensions, lblFile);
                }
                else
                {
                    listFiles = GetFiles(extensions, origin, SearchOption.AllDirectories);
                    listDirectories = GetDirectories(extensions, origin);

                    foreach (var dirPath in listDirectories)
                    {
                        var newPath = dirPath.Replace(origin, destination);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                        }
                    }

                    // hacemos este bucle dos veces para contar los archivos que al final se van a procesar
                    // el metodo "GetCountFiles" no vale porque saca todos los del origen, pero este metodo solo copia los que no estén en el destino
                    foreach (var originFile in listFiles)
                    {
                        var destinationFile = originFile.Replace(origin, destination);
                        if (!File.Exists(destinationFile))
                        {
                            totalFiles++;
                        }
                    }

                    foreach (var originfile in listFiles)
                    {
                        var destinationFile = originfile.Replace(origin, destination);
                        if (!File.Exists(destinationFile))
                        {
                            file = Path.GetFileName(originfile);
                            var sizeFile = (new FileInfo(originfile)).Length;

                            (sender as BackgroundWorker).ReportProgress((processedFiles * 100) / totalFiles);
                            processedFiles++;
                            lblFile.Dispatcher.Invoke(new Action(() =>
                            {
                                lblFile.Content = string.Format("Destination folder: {0}\tFiles to copy: {1}\nFiles copied: {2}\nSize: {3}\tCopying file: {4}", destination, totalFiles.ToString(), processedFiles.ToString(), ConvertBytes(sizeFile), file);
                            }));

                            File.Copy(originfile, destinationFile, true);
                        }
                    }
                    lblFile.Dispatcher.Invoke(() =>
                    {
                        lblFile.Content = string.Format("Destination folder: {0}\tFiles copied: {1}\nTime elapsed: {2} Days {3} Hours {4} Minutes {5} Seconds {6} Milliseconds", destination, totalFiles.ToString(), time.Elapsed.Days.ToString(), time.Elapsed.Hours.ToString(), time.Elapsed.Minutes.ToString(), time.Elapsed.Seconds.ToString(), time.Elapsed.Milliseconds.ToString());
                    });
                }

                // forzamos el valor 100 en el progressbar porque a veces se queda sin llenarse (y no sé porqué)
                (sender as BackgroundWorker).ReportProgress(100);
            }
            catch (Exception ex)
            {
                return file;
            }
            return file;
        }

        /// <summary>
        /// Funcion para hacer la copia "TYPE_BACKUP_COMPARE_ATTRIBUTES"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="extensions"></param>
        /// <param name="lblFile"></param>
        /// <param name="isOverwrite"></param>
        /// <param name="isAllFiles"></param>
        /// <returns></returns>
        public static string CopyComparingAttributes(object sender, string origin, string destination, List<string> extensions, Label lblFile, bool isOverwrite, bool isAllFiles)
        {
            var file = string.Empty;
            var totalFiles = 0;
            var processedFiles = 0;
            var time = Stopwatch.StartNew();
            IEnumerable<string> listFiles;
            IEnumerable<string> listDirectories;

            try
            {
                listFiles = GetFiles(extensions, origin, SearchOption.AllDirectories);
                listDirectories = GetDirectories(extensions, origin);

                foreach (string dirPath in listDirectories)
                {
                    var newPath = dirPath.Replace(origin, destination);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                }

                // hacemos este bucle dos veces para contar los archivos que al final se van a procesar
                // el metodo "GetCountFiles" no vale porque saca todos los del origen, pero este metodo solo copia los que no estén en el destino
                foreach (var originFile in listFiles)
                {
                    var pf = new ProcessedFile()
                    {
                        Name = Path.GetFileNameWithoutExtension(originFile),
                        FullName = Path.GetFileName(originFile),
                        Extension = Path.GetExtension(originFile),
                        OriginSize = (new FileInfo(originFile)).Length,
                        OriginModifyDate = File.GetLastWriteTime(originFile),
                        OriginDirectory = Path.GetDirectoryName(originFile),
                        OriginFullPath = originFile,
                        DestinationDirectory = Path.GetDirectoryName(originFile.Replace(origin, destination)),
                        DestinationFullPath = originFile.Replace(origin, destination),
                        DestinationSize = 0,
                        DestinationModifyDate = new DateTime(0)
                    };

                    // con esto revisamos que el nombre y la extension existe en destino
                    if (File.Exists(pf.DestinationFullPath))
                    {
                        // como existe, recogemos tamaño y fecha de modificacion
                        pf.DestinationSize = (new FileInfo(pf.DestinationFullPath)).Length;
                        pf.DestinationModifyDate = File.GetLastWriteTime(pf.DestinationFullPath);

                        //chequeamos y si es diferente, lo contabilizamos
                        if (pf.CheckFileExistsWithDistinctModifyDate())
                        {
                            totalFiles++;
                        }
                    }
                }

                if (totalFiles > 0)
                {
                    foreach (var originFile in listFiles)
                    {
                        var pf = new ProcessedFile()
                        {
                            Name = Path.GetFileNameWithoutExtension(originFile),
                            FullName = Path.GetFileName(originFile),
                            Extension = Path.GetExtension(originFile),
                            OriginSize = (new FileInfo(originFile)).Length,
                            OriginModifyDate = File.GetLastWriteTime(originFile),
                            OriginDirectory = Path.GetDirectoryName(originFile),
                            OriginFullPath = originFile,
                            DestinationDirectory = Path.GetDirectoryName(originFile.Replace(origin, destination)),
                            DestinationFullPath = originFile.Replace(origin, destination),
                            DestinationSize = 0,
                            DestinationModifyDate = new DateTime(0)
                        };

                        // con esto revisamos que el nombre y la extension existe en destino
                        if (File.Exists(pf.DestinationFullPath))
                        {
                            // como existe, recogemos tamaño y fecha de modificacion
                            pf.DestinationSize = (new FileInfo(pf.DestinationFullPath)).Length;
                            pf.DestinationModifyDate = File.GetLastWriteTime(pf.DestinationFullPath);
                            file = pf.FullName;
                            (sender as BackgroundWorker).ReportProgress((processedFiles * 100) / totalFiles);
                            lblFile.Dispatcher.Invoke(() =>
                            {
                                lblFile.Content = string.Format("Destination folder: {0}\tFiles to copy: {1}\nFiles copied: {2}\nSize: {3}\tCopying file: {4}", destination, totalFiles.ToString(), processedFiles.ToString(), ConvertBytes(pf.OriginSize), string.Format("{0}{1}", pf.Name, pf.Extension));
                            });

                            //chequeamos y si es diferente, lo contabilizamos
                            if (!pf.CheckFileExistsWithDistinctModifyDate())
                            {
                                if (!isOverwrite)
                                {
                                    pf.CreateNewName();
                                }

                                File.Copy(pf.OriginFullPath, pf.DestinationFullPath, isOverwrite);
                            }
                            processedFiles++;
                        }
                        else if (!File.Exists(pf.DestinationFullPath) && isAllFiles)
                        {
                            File.Copy(pf.OriginDirectory, pf.DestinationFullPath, false);
                            processedFiles++;
                        }
                    }
                    lblFile.Dispatcher.Invoke(() =>
                    {
                        lblFile.Content = string.Format("Destination folder: {0}\tFiles copied: {1}\nTime elapsed: {2} Days {3} Hours {4} Minutes {5} Seconds {6} Milliseconds", destination, totalFiles.ToString(), time.Elapsed.Days.ToString(), time.Elapsed.Hours.ToString(), time.Elapsed.Minutes.ToString(), time.Elapsed.Seconds.ToString(), time.Elapsed.Milliseconds.ToString());
                    });

                    // forzamos el valor 100 en el progressbar porque a veces se queda sin llenarse (y no sé porqué)
                    (sender as BackgroundWorker).ReportProgress(100);
                }
                else
                {
                    lblFile.Dispatcher.Invoke(() =>
                    {
                        lblFile.Content = Constants.LABEL_NO_FILES_TO_COPY;
                    });
                }
            }
            catch (Exception ex)
            {
                return file;
            }
            return file;
        }

        /// <summary>
        /// Funcion para hacer la copia "TYPE_BACKUP_COMPARE_HASH"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="extensions"></param>
        /// <param name="lblFile"></param>
        /// <param name="isOverwrite"></param>
        /// <param name="isAllFiles"></param>
        /// <returns></returns>
        public static string CopyComparingHash(object sender, string origin, string destination, List<string> extensions, Label lblFile, bool isOverwrite, bool isAllFiles)
        {
            var file = string.Empty;
            var totalFiles = 0;
            var processedFiles = 0;
            var time = Stopwatch.StartNew();

            try
            {
                var listDirectories = GetDirectories(extensions, origin).ToList();

                foreach (var dirPath in listDirectories)
                {
                    var destinationPath = dirPath.Replace(origin, destination);
                    if (Directory.Exists(destinationPath))
                    {
                        totalFiles += GetFiles(extensions, dirPath, SearchOption.TopDirectoryOnly).Count();
                    }
                }

                foreach (var dirPath in listDirectories)
                {
                    var destinationPath = dirPath.Replace(origin, destination);
                    if (Directory.Exists(destinationPath))
                    {
                        var listFileOrigin = GetFiles(extensions, dirPath, SearchOption.TopDirectoryOnly);

                        foreach (var originFile in listFileOrigin)
                        {
                            var pf = new ProcessedFile()
                            {
                                Name = Path.GetFileNameWithoutExtension(originFile),
                                FullName = Path.GetFileName(originFile),
                                Extension = Path.GetExtension(originFile),
                                OriginSize = (new FileInfo(originFile)).Length,
                                OriginModifyDate = File.GetLastWriteTime(originFile),
                                OriginDirectory = Path.GetDirectoryName(originFile),
                                OriginFullPath = originFile,
                                DestinationDirectory = Path.GetDirectoryName(originFile.Replace(origin, destination)),
                                DestinationFullPath = originFile.Replace(origin, destination),
                                DestinationSize = 0,
                                DestinationModifyDate = new DateTime(0)
                            };

                            var listFileDestination = GetFiles(extensions, pf.DestinationDirectory, SearchOption.TopDirectoryOnly);

                            foreach (var destinationFile in listFileDestination)
                            {
                                var compareResult = CompareFiles(Constants.COMPARE_FILE_SHA512, originFile, destinationFile);

                                if (compareResult == null) break;

                                if (compareResult == true)
                                {
                                    file = pf.Name;
                                    (sender as BackgroundWorker).ReportProgress((processedFiles * 100) / totalFiles);
                                    lblFile.Dispatcher.Invoke(() =>
                                    {
                                        lblFile.Content = string.Format(
                                            "Destination folder: {0}\tFiles to copy: {1}\nFiles copied: {2}\nSize: {3}\tCopying file: {4}",
                                            destination, totalFiles.ToString(), processedFiles.ToString(),
                                            ConvertBytes(pf.OriginSize),
                                            pf.FullName);
                                    });

                                    if (!isOverwrite)
                                    {
                                        pf.CreateNewName();
                                    }

                                    processedFiles++;

                                    File.Copy(pf.OriginFullPath, pf.DestinationFullPath, isOverwrite);
                                    break;
                                }
                                else if (!File.Exists(pf.DestinationFullPath) && isAllFiles)
                                {
                                    File.Copy(pf.OriginFullPath, pf.DestinationFullPath, false);
                                    processedFiles++;
                                }
                            }
                        }
                    }
                    else
                    {
                        CopyOriginNotInDestination(sender, origin, destination, extensions, lblFile);
                    }
                }

                lblFile.Dispatcher.Invoke(() =>
                {
                    lblFile.Content = string.Format(
                        "Destination folder: {0}\tFiles copied: {1}\nTime elapsed: {2} Days {3} Hours {4} Minutes {5} Seconds {6} Milliseconds",
                        destination, totalFiles.ToString(), time.Elapsed.Days.ToString(),
                        time.Elapsed.Hours.ToString(), time.Elapsed.Minutes.ToString(),
                        time.Elapsed.Seconds.ToString(), time.Elapsed.Milliseconds.ToString());
                });

                // forzamos el valor 100 en el progressbar porque a veces se queda sin llenarse (y no sé porqué)
                (sender as BackgroundWorker).ReportProgress(100);
            }
            catch (Exception ex)
            {
                return file;
            }
            return file;
        }

        /// <summary>
        /// Funcion que compara dos archivos por el tipo de algoritmo especificado (HASH o SHA512)
        /// </summary>
        /// <param name="typeAlgorithm"></param>
        /// <param name="originFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        public static bool? CompareFiles(string typeAlgorithm, string originFile, string destinationFile)
        {
            bool? result;

            switch (typeAlgorithm)
            {
                case Constants.COMPARE_FILE_HASH:
                    result = CompareHashFile(originFile, destinationFile);
                    break;

                case Constants.COMPARE_FILE_SHA512:
                    result = CompareSHA512(originFile, destinationFile);
                    break;

                default:
                    result = null;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Funcion que compara dos archivos por HASH
        /// </summary>
        /// <param name="originFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        private static bool CompareHashFile(string originFile, string destinationFile)
        {
            var hash = HashAlgorithm.Create();
            byte[] originFileHash;
            byte[] destinationFileHash;

            using (var originFileStream = new FileStream(originFile, FileMode.Open))
            using (var destinationFileStream = new FileStream(destinationFile, FileMode.Open))
            {
                originFileHash = hash.ComputeHash(originFileStream);
                destinationFileHash = hash.ComputeHash(destinationFileStream);
            }
            var result = BitConverter.ToString(originFileHash) == BitConverter.ToString(destinationFileHash);
            return result;
        }

        /// <summary>
        /// Funcion que compara dos archivos por SHA512
        /// </summary>
        /// <param name="originFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        public static bool CompareSHA512(string originFile, string destinationFile)
        {
            using (var sha512 = SHA512.Create())
            using (var originFileStream = new FileStream(originFile, FileMode.Open))
            using (var destinationFileStream = new FileStream(destinationFile, FileMode.Open))
            {
                var originFileHash = sha512.ComputeHash(originFileStream);
                var destinationFileHash = sha512.ComputeHash(destinationFileStream);
                var result = BitConverter.ToString(originFileHash) == BitConverter.ToString(destinationFileHash);
                return result;
            }
        }
    }
}