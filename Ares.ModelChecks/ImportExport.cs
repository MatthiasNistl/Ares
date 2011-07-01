﻿using System;
using System.Collections.Generic;

using Ares.Data;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace Ares.ModelInfo
{

    #region Import

    public class ProjectImporter
    {
        private ProgressMonitor m_Monitor;
        private System.ComponentModel.BackgroundWorker m_Worker;
        private System.Action m_ProjectLoadedFunc;

        public static void Import(System.Windows.Forms.Form parent, String importFileName, String projectFileName,
            System.Action projectLoaded)
        {
            ProjectImporter importer = new ProjectImporter();
            importer.DoImport(parent, importFileName, projectFileName, projectLoaded);
        }

        private ProjectImporter()
        {
        }

        private void DoImport(System.Windows.Forms.Form parent, String importFileName, String projectFileName,
            System.Action projectLoaded)
        {
            m_ProjectLoadedFunc = projectLoaded;
            try
            {
                long overallSize = 0;
                bool overWrite = false;
                bool hasAskedForOverwrite = false;
                bool hasProjectFile = false;
                using (ZipFile file = new ZipFile(importFileName))
                {
                    for (int i = 0; i < file.Count; ++i)
                    {
                        ZipEntry entry = file[i];
                        if (entry.IsDirectory)
                            continue;
                        String fileName = GetFileName(entry);
                        if (fileName == String.Empty)
                        {
                            if (hasProjectFile)
                            {
                                throw new ArgumentException(StringResources.InvalidImportFile);
                            }
                            else
                            {
                                hasProjectFile = true;
                                overallSize += entry.Size;
                            }
                        }
                        else if (System.IO.File.Exists(fileName))
                        {
                            if (!hasAskedForOverwrite)
                            {
                                switch (System.Windows.Forms.MessageBox.Show(StringResources.ImportOverwrite, StringResources.Ares,
                                    System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question))
                                {
                                    case System.Windows.Forms.DialogResult.Yes:
                                        overWrite = true;
                                        hasAskedForOverwrite = true;
                                        break;
                                    case System.Windows.Forms.DialogResult.No:
                                        overWrite = false;
                                        hasAskedForOverwrite = true;
                                        break;
                                    default:
                                        return;
                                }
                            }
                            if (overWrite)
                                overallSize += entry.Size;
                        }
                        else
                        {
                            overallSize += entry.Size;
                        }
                    }
                    if (!hasProjectFile)
                    {
                        throw new ArgumentException(StringResources.InvalidImportFile);
                    }
                }
                ProjectImportData data = new ProjectImportData();
                data.Overwrite = overWrite;
                data.ImportFile = importFileName;
                data.ProjectFile = projectFileName;
                data.OverallSize = overallSize;
                m_Worker = new System.ComponentModel.BackgroundWorker();
                m_Worker.WorkerReportsProgress = true;
                m_Worker.WorkerSupportsCancellation = true;
                m_Worker.DoWork += new System.ComponentModel.DoWorkEventHandler(m_Worker_DoWork);
                m_Worker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(m_Worker_ProgressChanged);
                m_Worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(m_Worker_RunWorkerCompleted);
                m_Monitor = new ProgressMonitor(parent, StringResources.Importing);
                m_Worker.RunWorkerAsync(data);

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(String.Format(StringResources.ImportError, ex.Message), StringResources.Ares,
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        void m_Worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            m_Monitor.Close();
            if (e.Error != null)
            {
                System.Windows.Forms.MessageBox.Show(String.Format(StringResources.ImportError, e.Error.Message), StringResources.Ares,
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            else if (!e.Cancelled)
            {
                m_ProjectLoadedFunc();
            }
        }

        void m_Worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (m_Monitor.Canceled)
            {
                m_Worker.CancelAsync();
            }
            else
            {
                m_Monitor.SetProgress(e.ProgressPercentage, e.UserState.ToString());
            }
        }

        void m_Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ProjectImportData data = (ProjectImportData)e.Argument;
            using (ZipInputStream stream = new ZipInputStream(System.IO.File.OpenRead(data.ImportFile)))
            {
                ZipEntry entry;
                byte[] buffer = new byte[4096];
                long writtenSize = 0;
                int lastPercent = -1;
                String lastFile = String.Empty;
                while ((entry = stream.GetNextEntry()) != null)
                {
                    String fileName = GetFileName(entry);
                    if (fileName == String.Empty)
                    {
                        fileName = data.ProjectFile;
                    }
                    else if (!data.Overwrite && System.IO.File.Exists(fileName))
                    {
                        continue;
                    }
                    String directoryName = System.IO.Path.GetDirectoryName(fileName);
                    if (directoryName.Length > 0)
                    {
                        System.IO.Directory.CreateDirectory(directoryName);
                    }
                    using (System.IO.FileStream fileStream = System.IO.File.Create(fileName))
                    {
                        StreamUtils.Copy(stream, fileStream, buffer, new ProgressHandler((object o, ProgressEventArgs e2) =>
                            {
                                long currentSize = writtenSize + e2.Processed;
                                int currentPercent = (int)(((double)currentSize / (double)data.OverallSize) * 100.0);
                                if (currentPercent != lastPercent || e2.Name != lastFile)
                                {
                                    lastPercent = currentPercent;
                                    lastFile = e2.Name;
                                    m_Worker.ReportProgress(lastPercent, lastFile);
                                    e2.ContinueRunning = !m_Worker.CancellationPending;
                                }
                            }), TimeSpan.FromMilliseconds(50), entry, fileName, entry.Size);
                    }
                    writtenSize += entry.Size;
                    if (m_Worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
            }
        }

        class ProjectImportData
        {
            public bool Overwrite { get; set; }
            public String ImportFile { get; set; }
            public String ProjectFile { get; set; }
            public long OverallSize { get; set; }
        }

        private String GetFileName(ZipEntry entry)
        {
            String fileName = String.Empty;
            if (entry.Name.StartsWith(ProjectExporter.MUSIC_DIR))
            {
                fileName = entry.Name.Substring(ProjectExporter.MUSIC_DIR.Length + 1);
                fileName = fileName.Replace('/', System.IO.Path.DirectorySeparatorChar);
                fileName = System.IO.Path.Combine(Settings.Settings.Instance.MusicDirectory, fileName);
            }
            else if (entry.Name.StartsWith(ProjectExporter.SOUND_DIR))
            {
                fileName = entry.Name.Substring(ProjectExporter.SOUND_DIR.Length + 1);
                fileName = fileName.Replace('/', System.IO.Path.DirectorySeparatorChar);
                fileName = System.IO.Path.Combine(Settings.Settings.Instance.SoundDirectory, fileName);
            }
            return fileName;
        }
    }

    #endregion

    #region Export

    public class ProjectExporter
    {
        private ProgressMonitor m_Monitor;
        private System.ComponentModel.BackgroundWorker m_Worker;

        public static void Export(System.Windows.Forms.Form parent, IProject project, String exportFileName)
        {
            ProjectExporter exporter = new ProjectExporter();
            exporter.DoExport(parent, project, exportFileName);
        }

        private ProjectExporter()
        {
        }

        private void DoExport(System.Windows.Forms.Form parent, IProject project, String exportFileName)
        {
            ProjectExportData data = new ProjectExportData();
            data.Project = project;
            data.ProjectFileName = project.FileName;
            data.ExportFileName = exportFileName;
            m_Worker = new System.ComponentModel.BackgroundWorker();
            m_Worker.WorkerReportsProgress = true;
            m_Worker.WorkerSupportsCancellation = true;
            m_Worker.DoWork += new System.ComponentModel.DoWorkEventHandler(m_Worker_DoWork);
            m_Worker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(m_Worker_ProgressChanged);
            m_Worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(m_Worker_RunWorkerCompleted);
            m_Monitor = new ProgressMonitor(parent, StringResources.Exporting);
            m_Worker.RunWorkerAsync(data);
        }

        void m_Worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            m_Monitor.Close();
            if (e.Error != null)
            {
                System.Windows.Forms.MessageBox.Show(String.Format(StringResources.ExportError, e.Error.Message), StringResources.Ares,
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        void m_Worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (m_Monitor.Canceled)
            {
                m_Worker.CancelAsync();
            }
            else
            {
                m_Monitor.SetProgress(e.ProgressPercentage, e.UserState.ToString());
            }
        }

        public static String MUSIC_DIR = "Music";
        public static String SOUND_DIR = "Sounds";

        class ProjectExportData
        {
            public IProject Project { get; set; }
            public String ProjectFileName { get; set; }
            public String ExportFileName { get; set; }
        }

        void m_Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ProjectExportData data = (ProjectExportData)e.Argument;
            long overallSize = 0;

            // collect entries and calculate overall size
            Dictionary<ZipEntry, String> zipEntries = new Dictionary<ZipEntry, string>();
            Dictionary<ZipEntry, long> zipSizes = new Dictionary<ZipEntry, long>();

            // project file: no path
            ZipEntry entry = new ZipEntry(System.IO.Path.GetFileName(data.ProjectFileName));
            long size;
            SetZipEntryAttributes(entry, data.ProjectFileName, out size);
            zipEntries[entry] = data.ProjectFileName;
            zipSizes[entry] = size;
            overallSize += size;

            // entries for the music and sound files
            Ares.ModelInfo.FileLists fileLists = new Ares.ModelInfo.FileLists();
            foreach (IFileElement element in fileLists.GetAllFiles(data.Project))
            {
                String name = element.SoundFileType == SoundFileType.Music ? MUSIC_DIR : SOUND_DIR;
                name = System.IO.Path.Combine(name, element.FilePath);
                String fileName = element.SoundFileType == SoundFileType.Music ?
                    Settings.Settings.Instance.MusicDirectory : Settings.Settings.Instance.SoundDirectory;
                fileName = System.IO.Path.Combine(fileName, element.FilePath);
                ZipEntry fileEntry = new ZipEntry(ZipEntry.CleanName(name));
                SetZipEntryAttributes(fileEntry, fileName, out size);
                zipEntries[fileEntry] = fileName;
                zipSizes[fileEntry] = size;
                overallSize += size;
            }

            // now write zip file
            long writtenSize = 0;
            int lastPercent = -1;
            String lastFile = String.Empty;

            using (ZipOutputStream stream = new ZipOutputStream(System.IO.File.Create(data.ExportFileName)))
            {
                stream.SetLevel(7);
                byte[] buffer = new byte[4096];
                foreach (KeyValuePair<ZipEntry, String> zipEntry in zipEntries)
                {
                    stream.PutNextEntry(zipEntry.Key);
                    using (System.IO.FileStream fileStream = System.IO.File.OpenRead(zipEntry.Value))
                    {
                        StreamUtils.Copy(fileStream, stream, buffer, new ProgressHandler((object o, ProgressEventArgs e2) =>
                        {
                            long currentSize = writtenSize + e2.Processed;
                            int currentPercent = (int)(((double)currentSize / (double)overallSize) * 100.0);
                            if (currentPercent != lastPercent || e2.Name != lastFile)
                            {
                                lastPercent = currentPercent;
                                lastFile = e2.Name;
                                m_Worker.ReportProgress(lastPercent, lastFile);
                                e2.ContinueRunning = !m_Worker.CancellationPending;
                            }
                        }), TimeSpan.FromMilliseconds(50), zipEntry.Key, zipEntry.Value, zipEntry.Key.Size);
                    }
                    writtenSize += zipSizes[zipEntry.Key];
                    if (m_Worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                }
                stream.Finish();
                stream.Close();
            }
        }

        private static void SetZipEntryAttributes(ZipEntry entry, String file, out long size)
        {
            entry.CompressionMethod = CompressionMethod.Deflated;
            entry.DateTime = System.IO.File.GetLastWriteTime(file);
            size = (new System.IO.FileInfo(file)).Length;
        }

    }

    #endregion

    #region Progress

    class ProgressMonitor
    {
        private System.Timers.Timer timer;
        private bool canceled;
        private bool closed;
        private ProgressDialog dialog;
        private System.Windows.Forms.Form parent;

        private const int DELAY = 700;

        private Object syncObject = new object();

        public ProgressMonitor(System.Windows.Forms.Form parent, String text)
        {
            dialog = new ProgressDialog(text);
            dialog.Text = text;
            closed = false;
            canceled = false;
            this.parent = parent;
            timer = new System.Timers.Timer(DELAY);
            timer.AutoReset = false;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (syncObject)
            {
                if (closed)
                    return;
            }
            if (parent.InvokeRequired)
            {
                parent.Invoke(new Action(() => { OpenDialog(); }));
            }
            else
            {
                OpenDialog();
            }
        }

        private void OpenDialog()
        {
            lock (syncObject)
            {
                if (closed)
                    return;
            }
            DialogClosed(dialog.ShowDialog(parent));
        }

        private void DialogClosed(System.Windows.Forms.DialogResult result)
        {
            lock (syncObject)
            {
                if (!closed)
                    canceled = true;
            }
        }

        public bool Canceled
        {
            get
            {
                lock (syncObject)
                {
                    return canceled;
                }
            }
        }

        public void Close()
        {
            lock (syncObject)
            {
                closed = true;
                if (timer.Enabled)
                {
                    timer.Stop();
                }
            }
            if (dialog != null && dialog.InvokeRequired)
            {
                dialog.Invoke(new Action(() => { Close(); }));
            }
            else
            {
                if (dialog != null && dialog.Visible)
                {
                    dialog.Visible = false;
                    dialog.Close();
                }
                timer.Dispose();
            }
        }

        public void SetProgress(int progress, String text)
        {
            if (dialog.InvokeRequired)
            {
                dialog.Invoke(new Action(() => { dialog.SetProgress(progress, text); }));
            }
            else
            {
                dialog.SetProgress(progress, text);
            }
        }
    }

    #endregion
}