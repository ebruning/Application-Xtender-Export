using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kofax.Eclipse.Base;

namespace Kofax.Eclipse.AxRelease
{
    class AxIndexGenerator : IDocumentIndexGenerator
    {
        private static readonly Guid guid = new Guid("{22589961-4AA2-4c1b-8EB4-23F1A5254C3A}");
        private ReleaseMode m_ReleaseMode;

        #region Settings
        public void SerializeSettings(Stream output)
        {
            return;
        }

        public void DeserializeSettings(Stream input)
        {
            return;
        }

        public void Setup(IDictionary<string, string> releaseData)
        {
            return;
        } 
        #endregion
    
        public Guid Id
        {
            get { return guid; }
        }

        public string Name
        {
            get { return "ApplicationXtender Index Generator"; }
        }

        public string Description
        {
            get { return "Creates the text file for ApplicationXtender"; }
        }

        public string DefaultExtension
        {
            get { return string.Empty; }
        }

        public bool IsCustomizable
        {
            get { return false; }
        }


        #region Implementation of IDocumentIndexGenerator

        public bool IsSupported(ReleaseMode mode)
        {
            return mode == ReleaseMode.SinglePage;
        }

        public void SerializeSample(IDictionary<string, string> releaseData, Stream output)
        {
            return;
        }

        public void CreateIndex(IDocument document, IDictionary<string, string> releaseData, string outputFileName)
        {
            string index = "";

            using (FileStream stream = new FileStream(outputFileName, FileMode.Append, 
                                                      FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                //There has to be a better way to prevent the pipe at the end of the string
                for (int i = 0; i < document.IndexDataCount; i++)
                {
                    if (i == 0)
                        index += document.GetIndexDataValue(i);
                    else
                        index += "|" + document.GetIndexDataValue(i);
                }

                writer.WriteLine(index);

                foreach (KeyValuePair<string, string> file in releaseData)
                {
                    writer.WriteLine("@{0}", file.Value);
                }
                writer.Flush();
                writer.Close();
            }
            
        }

        public ReleaseMode WorkingMode
        {
            get { return m_ReleaseMode; }
            set 
            {
                m_ReleaseMode = value;
               
            }
        }

        #endregion
    }
}
