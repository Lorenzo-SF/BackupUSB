using System;

namespace BackupUSB
{
    public class ProcessedFile
    {
        public string Name;
        public string FullName;
        public string Extension;

        public string OriginDirectory;
        public string OriginFullPath;
        public long OriginSize;
        public DateTime OriginModifyDate;

        public string DestinationDirectory;
        public string DestinationFullPath;
        public long DestinationSize;
        public DateTime DestinationModifyDate;
        

        /// <summary>
        /// Revisa si el archivo de origen y el de destino tienen el mismo tamaño y fecha de modificacion.
        /// </summary>
        /// <returns></returns>
        public bool CheckFileExistsWithDistinctModifyDate() {
            return (OriginSize == DestinationSize && DateTime.Compare(OriginModifyDate, DestinationModifyDate) != 0);
        }

        public void CreateNewName() {
            DestinationFullPath = DestinationFullPath.Replace(FullName, string.Format("{0}_{1:yyyyMMddHHmmss}{2}", Name, DateTime.Now, Extension))
;        }
    }
}
