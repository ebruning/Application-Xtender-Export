using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Kofax.Eclipse.Base;

namespace Kofax.Eclipse.AxRelease
{
    public class AxRelease : IReleaseScript
    {
        #region Standard interface to inspect the script's properties and basic settings
        /// <summary>
        /// This GUID is used to uniquely identify the script and distinguish it from the application. 
        /// Generate a GUID from Visual Studio when you first create the script and 
        /// keep it for the rest of its life.
        /// </summary>
        public Guid Id
        {
            get { return new Guid("{96948FB5-8E66-4548-A4BC-464FB0A9EC8D}"); }
        }

        /// <summary>
        /// This name appears in the list of available release scripts 
        /// in the application's UI. The name should be localized.
        /// </summary>
        public string Name
        {
            get { return "ApplicationXtender Release"; }
        }

        /// <summary>
        /// This description appears whenever the application decides to 
        /// briefly explain the script's purpose to the user. The description should also be localized.
        /// </summary>
        public string Description
        {
            get { return "Release script for ApplicationXtender."; }
        }

        /// <summary>
        /// The script will release batches using the mode it remembered from a previous setup.
        /// </summary>
        public ReleaseMode WorkingMode
        {
            get { return m_WorkingMode; }
        }

        /// <summary>
        /// This simple script will process batches in both single-page and multi-page release modes.
        /// </summary>
        public bool IsSupported(ReleaseMode mode)
        {
            return true;
        } 
        #endregion

        #region Script settings - Will be remembered across sessions
        /// <summary>
        /// The script will release batches in this mode. The script can be configured by the setup dialog and remembered across sessions.
        /// </summary>
        private ReleaseMode m_WorkingMode = ReleaseMode.SinglePage;

        /// <summary>
        /// The destination to place the released pages. Under this destination, 
        /// a folder structure of "[BatchName]\[DocNumber]" will be created.
        /// </summary>
        private string m_Destination = string.Empty;

        /// <summary>
        /// ID of the user-selected file type converter to convert the released documents/pages.
        /// </summary>
        private Guid m_FileTypeId = Guid.Empty;

        /// <summary>
        /// Holds the name of the index file
        /// </summary>
        private string m_IndexFileName = string.Empty;
        #endregion

        #region Instance settings - Valid only throughout the running session
        /// <summary>
        /// Reference to the actively employed converter to pass the pages through
        /// </summary>
        private IPageOutputConverter m_PageConverter;

        /// <summary>
        /// Reference to the actively employed converter to pass the documents through
        /// </summary>
        private IDocumentOutputConverter m_DocConverter;

        /// <summary>
        /// Point to the destination folder for the entire released batch
        /// </summary>
        private string m_BatchFolder;

        /// <summary>
        /// Point to the destination folder for the pages to be released in the current document
        /// </summary>
        private string m_DocFolder;

        /// <summary>
        /// Dictionary to hold all file names being release
        /// </summary>
        IDictionary<string, string> m_FilenameReleaseData = new Dictionary<string, string>();

        /// <summary>
        /// Keeps track of the number of images in the batch
        /// </summary>
        private int m_FileCount = 1;
         
        /// <summary>
        /// Document folder name incremented every 1000 images
        /// </summary>
        private int m_DocFolderCount = -1;

        /// <summary>
        /// Variable to hold value for when a new document folder should be created
        /// </summary>
        private Int32 m_MaxImageFiles;

        /// <summary>
        /// Reference to the AX Index generator
        /// </summary>
        private AxIndexGenerator index;
        #endregion

        #region Handlers to be called during an actual release process
        /// <summary>
        /// This method will be called first when the release is started. The application will pass the latest information 
        /// from the running instance to the script through given parameters. The script should do its final check
        /// for proper release conditions, throwing exceptions if problems occur.
        /// </summary>
        public object StartRelease(IList<IExporter> exporters, IIndexField[] indexFields, IDictionary<string, string> releaseData)
        {
            if (string.IsNullOrEmpty(m_Destination))
                throw new Exception("Please specify a release destination");

            if (string.IsNullOrEmpty(m_IndexFileName))
                throw new Exception("Please specify an index file name");

            m_DocConverter = null;
            m_PageConverter = null;
            
            index = new AxIndexGenerator();

            foreach (IExporter exporter in exporters)
            {
                if (exporter.Id == m_FileTypeId)
                {
                    if (m_WorkingMode == ReleaseMode.SinglePage)
                        m_PageConverter = exporter as IPageOutputConverter;
                    else
                        m_DocConverter = exporter as IDocumentOutputConverter;
                }
            }

            /// When both of them can't be found, either the user hasn't set up properly, or the chosen converter has disappeared.
            /// The script can declare that the release cannot continue or proceed with default settings.
            if (m_PageConverter == null && m_DocConverter == null)
                throw new Exception("Please select an output file type");

            /// The application will keep any object returned from this function and pass it back to the script 
            /// in the EndRelease call. This is usually intended to facilitate cleanup.
            return null;
        }

        /// <summary>
        /// This method will be called after the batch has been prepared by the application 
        /// but before any document/page is sent to the script. The scripts usually perform 
        /// preparations to release the batch based on the current settings.
        /// </summary>
        public object StartBatch(IBatch batch)
        {
            m_BatchFolder = Path.Combine(m_Destination, batch.Name);

            bool batchFolderCreated = !Directory.Exists(m_BatchFolder);
            if (batchFolderCreated)
                Directory.CreateDirectory(m_BatchFolder);

            /// Again, the application will keep any object returned from this function and pass it back to the script 
            /// in the EndBatch call. This is usually intended to facilitate cleanup.
            return batchFolderCreated;
        }

        /// <summary>
        /// In multipage release mode, this method will be called for every document in the batch.
        /// This script will simply pass documents to the selected document output converter to produce
        /// the expected output files in the released batch folder.
        /// </summary>
        public void Release(IDocument doc)
        {
            CheckImageCountCreateFolder();

            string outputFileName = Path.Combine(m_DocFolder, m_FileCount.ToString().PadLeft(8, '0'));
            m_DocConverter.Convert(doc, Path.ChangeExtension(outputFileName, m_DocConverter.DefaultExtension));

            m_FilenameReleaseData.Add("Filename" + m_FileCount, Path.ChangeExtension(outputFileName, m_DocConverter.DefaultExtension));
            m_FileCount++;
            
            index.CreateIndex(doc, m_FilenameReleaseData, Path.Combine(m_BatchFolder, m_IndexFileName));

            m_FilenameReleaseData.Clear();
        }

        /// <summary>
        /// For every document, this method will be called after it has been prepared by the application 
        /// but before any page is sent to the script. The scripts usually performs preparations to release
        /// the document based on the current settings.
        /// </summary>
        public object StartDocument(IDocument doc)
        {
            return null;
        }

        /// <summary>
        /// Releases single page images into folders of 1000
        /// Creates a new document folder for each group of 1000 images
        /// </summary>
        public void Release(IPage page)
        {
            CheckImageCountCreateFolder();

            string outputFileName = Path.Combine(m_DocFolder, m_FileCount.ToString().PadLeft(8,'0'));
            m_PageConverter.Convert(page, Path.ChangeExtension(outputFileName, m_PageConverter.DefaultExtension));

            m_FilenameReleaseData.Add("Filename" + m_FileCount, Path.ChangeExtension(outputFileName, m_PageConverter.DefaultExtension));
            m_FileCount++;
        }

        /// <summary>
        /// Creates the index file since we have the path to all images
        /// </summary>
        public void EndDocument(IDocument doc, object handle, ReleaseResult result)
        {
            /// The handle should always indicate whether or not the script created the document folder from scratch
            if (result != ReleaseResult.Succeeded && (bool)handle)
                Directory.Delete(m_DocFolder);
           
            index.CreateIndex(doc, m_FilenameReleaseData, Path.Combine(m_BatchFolder, m_IndexFileName));

            m_FilenameReleaseData.Clear();

        }

        /// <summary>
        /// This method will be called after all documents have been sent to the script. 
        /// The scripts usually perform the necessary cleanup based on current settings 
        /// and the actual release conditions.
        /// </summary>
        public void EndBatch(IBatch batch, object handle, ReleaseResult result)
        {
            /// The handle should always indicate whether or not the script created the batch folder from scratch
            if (result != ReleaseResult.Succeeded && (bool)handle)
                Directory.Delete(m_BatchFolder);
        }

        /// <summary>
        /// This method will be called after everything has been sent to the script 
        /// and the batch has been closed by the application. The scripts usually perform 
        /// necessary cleanup based on current settings and the actual release conditions.
        /// </summary>
        public void EndRelease(object handle, ReleaseResult result)
        {
            /// Since we don't do anything special in this simple script, 
            /// there's nothing to be cleaned up here. The handle should always be null.
        } 
        #endregion

        #region Handlers to be called during configuration requests by the user and before/after a release session
        /// <summary>
        /// Simply write whatever needs to persist across sessions here.
        /// </summary>
        public void SerializeSettings(Stream output)
        {
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                writer.Write(m_Destination);
                writer.Write(m_FileTypeId.ToString());
                writer.Write(m_WorkingMode.ToString());
                writer.Write(m_IndexFileName);
                writer.Write(m_MaxImageFiles);
            }
        }

        /// <summary>
        /// Simply read whatever persisted from previous sessions here.
        /// </summary>
        public void DeserializeSettings(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input))
            {
                try
                {
                    m_Destination = reader.ReadString();
                    m_FileTypeId = new Guid(reader.ReadString());
                    m_WorkingMode = (ReleaseMode)Enum.Parse(typeof(ReleaseMode), reader.ReadString());
                    m_IndexFileName = reader.ReadString();
                    //TODO: this works when the number is under 127. Need to handle large integers
                    m_MaxImageFiles = reader.ReadInt32();
                }
                catch
                {
                    /// If the script throws exceptions here, it wouldn't be able to recover from the application.
                    /// This will be addressed in a later version of the API.
                    m_Destination = string.Empty;
                    m_FileTypeId = Guid.Empty;
                    m_WorkingMode = ReleaseMode.SinglePage;
                    m_IndexFileName = string.Empty;
                    m_MaxImageFiles = 0;
                }
            }
        }

        /// <summary>
        /// Whenever the user requests to configure the script's settings, the method will be called with
        /// the latest information from the application's running instance as parameters. Also, a script can
        /// define and add its own information to the data table and pass it further down to the exporters.
        /// </summary>
        public void Setup(IList<IExporter> exporters, IIndexField[] indexFields, IDictionary<string, string> releaseData)
        {
            AxReleaseSetup setupDialog = new AxReleaseSetup(exporters, m_Destination, 
                                                            m_FileTypeId, m_WorkingMode, 
                                                            m_IndexFileName, m_MaxImageFiles);

            if (setupDialog.ShowDialog() != DialogResult.OK) return;

            m_Destination = setupDialog.Destination;
            m_FileTypeId = setupDialog.FileTypeId;
            m_WorkingMode = setupDialog.WorkingMode;
            m_IndexFileName = setupDialog.IndexFileName;
            m_MaxImageFiles = Int32.Parse(setupDialog.MaxImageFiles);
        } 
        #endregion

        public void CheckImageCountCreateFolder()
        {
            //TODO: This is a little kludgey; look at making it cleaner
            if (m_FileCount % (m_MaxImageFiles - 1) == 0)
                m_DocFolderCount++;

            m_DocFolder = Path.Combine(m_BatchFolder, m_DocFolderCount.ToString().PadLeft(5, '0'));

            //Moved this to Release(IPage) since we are only releasing single pages and allows us to keep track of the number if images
            bool docFolderCreated = !Directory.Exists(m_DocFolder);
            if (docFolderCreated)
                Directory.CreateDirectory(m_DocFolder); 
        }
    }
}