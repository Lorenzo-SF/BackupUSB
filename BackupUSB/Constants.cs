
namespace BackupUSB
{
    public static class Constants
    {
        public const string LABEL_ORIGIN                                = "Origen";
        public const string LABEL_ORIGIN_OK                             = "Origen OK";
        public const string LABEL_ORIGIN_EMPTY                          = "Origen vacío";
        public const string LABEL_ORIGIN_NOT_EXISTS                     = "Origen no existe";
        public const string LABEL_NO_FILES_TO_COPY                      = "No hay diferencias entre origen y destino. No hay nada que copiar.";

        public const string LABEL_DESTINATION                           = "Destino";
        public const string LABEL_DESTINATION_OK                        = "Destino OK";
        public const string LABEL_DESTINATION_EMPTY                     = "Destino no seleccionado";
        public const string LABEL_DESTINATION_EXISTS                    = "Destino existe (debe borrarse)";
        public const string LABEL_DESTINATION_NOT_VALID                 = "Destino no válido";

        public const string LABEL_TYPE_STATUS                           = "Estado (a la espera)";
        public const string LABEL_TYPE_STATUS_OK                        = "Estado (completado)";
        public const string LABEL_TYPE_STATUS_WITH_ERRORS               = "Estado (con errores)";
        public const string LABEL_TYPE_STATUS_PROCESING                 = "Estado (copiando)";

        public const string LABEL_TYPE_BACKUP                           = "Tipo Backup";
        public const string LABEL_TYPE_BACKUP_OK                        = "Tipo Backup OK";
        public const string LABEL_TYPE_BACKUP_NOT_SELECTED              = "Tipo Backup (seleccione tipo de backup)";
        public const string LABEL_TYPE_COPY_NOT_SELECTED                = "Tipo Backup (seleccione tipo de copia)";
        public const string LABEL_QUANTITY_FILES_NOT_SELECTED           = "Tipo Backup (seleccione cantidad de archivos)";

        public const string BUTTON_SHORTCUT_BACKUP_PRINCIPAL_FILES      = "btnPathPrimary";
        public const string BUTTON_SHORTCUT_BACKUP_AUTOREPLACEMENTS     = "btnPathSecondary";
        public const string BUTTON_BROWSE_ORIGIN_PATH                   = "btnBrowseOriginPath";

        public const string COMBO_DRIVES_LIST                           = "cmbDrives";
        public const string COMBO_TYPE_BACKUP                           = "cmbTipoBackup";
        public const string COMBO_TYPE_COPY                             = "cmbTipoTratamiento";

        public const string PATTERN_ALL_FILES                           = ".*";
        public const string BACKSLASH                                   = "\\";

        public const string TYPE_BACKUP_REPLACE_ORIGIN_DESTINATION      = "Sustituir archivos de destino por archivos de origen";
        public const string TYPE_BACKUP_COPY_ORIGIN_NOT_IN_DESTINATION  = "Copiar archivos de origen que no esté en destino" ;
        public const string TYPE_BACKUP_COMPARE_ATTRIBUTES              = "Comparar por directorio (archivos por nombre, extension, tamaño y fecha de modificacion.)";
        public const string TYPE_BACKUP_COMPARE_HASH                    = "Comparar por directorio (archivos por hash)";        

        public const string TYPE_COPY_OVERWRITE                         = "Sobreescribir destino";
        public const string TYPE_COPY_ADD_NEW_FILE                      = "Añadir (modifica nombre destino)";

        public const string TYPE_COPY_ALL_FILLES                        = "Todos los archivos";
        public const string TYPE_COPY_ONLY_COMMON_FILES                 = "Solo comunes";

        public const string SOME_EXTENSIONS_SELECTED                    = "Cantidad extensiones: {0}";
        public const string ALL_EXTENSIONS_SELECTED                     = "Todas las extensiones";

        public const string COMPARE_FILE_HASH                           = "HASH";
        public const string COMPARE_FILE_SHA512                         = "SHA512";
    }
}
