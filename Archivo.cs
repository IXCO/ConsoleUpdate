using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Xml;
using System.Data.OleDb;
using System.IO;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
namespace ConsoleUpdate
{
    class Archivo
    {
        public String nameOfPDFFile;
        public String nameOfXMLFile;
        private String backupDirectory = "C:\\facturasDown\\";
        public Archivo(String uuid)
        {
            //Generates name of the files according to the id that recevies plus the extension
            nameOfXMLFile = uuid + ".xml";
            nameOfPDFFile = uuid + ".pdf";
        }
        public Archivo()
        {
        }
        public String hasPDFForAttachment(String uuid)
        {
            if (File.Exists(backupDirectory + uuid + ".pdf"))
            {
                return backupDirectory + uuid + ".pdf";
            }
            else
            {
                return null;
            }
        }

        
    }
}
