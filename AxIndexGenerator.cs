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

        public AxIndexGenerator(ReleaseMode mode)
        {
            WorkingMode = mode;
        }

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
            //return mode == ReleaseMode.SinglePage;
            return true;
        }

        public void SerializeSample(IDictionary<string, string> releaseData, Stream output)
        {
            return;
        }

        public void CreateIndex(IDocument document, IDictionary<string, string> releaseData, string outputFileName)
        {
            string index = string.Empty;

            using (FileStream stream = new FileStream(outputFileName, FileMode.Append, 
                                                      FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                for (int i = 0; i < document.IndexDataCount; i++)
                    index += string.Format("{0},", document.GetIndexDataValue(i));

                foreach (KeyValuePair<string, string> file in releaseData)
                    if (WorkingMode == ReleaseMode.SinglePage)
                        writer.WriteLine(string.Format("{0}@{1}", index.TrimEnd(','), file.Value));
                    else
                        writer.WriteLine(string.Format("{0}@@{1}", index.TrimEnd(','), file.Value));


                writer.Flush();
                writer.Close();
            }
        }

        public ReleaseMode WorkingMode { get; set; }

        #endregion
    }
}
