﻿/*
 Copyright (c) 2015 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.Xml;
#if ANDROID
using Ares.Settings;
#endif

namespace Ares.Data
{

    /// <summary>
    /// Exception thrown when opening a file which doesn't contain a project at all.
    /// </summary>
    public class InvalidProjectException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public InvalidProjectException() : base() { }
    }

    /// <summary>
    /// Management of ARES projects.
    /// </summary>
    public interface IProjectManager
    {
        /// <summary>
        /// Creates a new project.
        /// </summary>
        IProject CreateProject(String title);

        /// <summary>
        /// Loads a project from a file.
        /// </summary>
        IProject LoadProject(String fileName);

        /// <summary>
        /// Saves a project. The file name must already be set.
        /// </summary>
        void SaveProject(IProject project);

        /// <summary>
        /// Saves a project to a file.
        /// </summary>
        void SaveProject(IProject project, String fileName);

        /// <summary>
        /// Unloads a project.
        /// </summary>
        void UnloadProject(IProject project);

        /// <summary>
        /// Exports something to an XML file
        /// </summary>
        /// <param name="elements">the elements</param>
        /// <param name="fileName">the file</param>
        void ExportElements(IList<IXmlWritable> elements, String fileName);

        /// <summary>
        /// Exports something to an XML string
        /// </summary>
        /// <param name="elements">the elements</param>
        /// <param name="output">the destination string</param>
        void ExportElements(IList<IXmlWritable> elements, System.Text.StringBuilder output);

        /// <summary>
        /// Reads something from an XML file
        /// </summary>
        /// <param name="fileName">the file</param>
        /// <returns>the read elements, or null</returns>
        IList<IXmlWritable> ImportElements(String fileName);

        /// <summary>
        /// Reads something from a string
        /// </summary>
        /// <param name="serializedForm">serialized data from ExportElements</param>
        /// <returns>the read elements, or null</returns>
        IList<IXmlWritable> ImportElementsFromString(String serializedForm);
    }

	public static class FileHelpers
	{
		public static System.IO.Stream GetFileContentStream(String filePath)
		{
			#if ANDROID
			if (filePath.IsSmbFile())
			{
				/*
				using (var stream = SambaHelpers.GetSambaInputStream(filePath))
				{
					var bytes = new byte[stream.Length];
					stream.Read(bytes, 0, bytes.Length);
					return new System.IO.MemoryStream(bytes);
				}
				*/
				return SambaHelpers.GetSambaInputStream(filePath);
			}
			#endif
			return new System.IO.FileStream(filePath, System.IO.FileMode.Open);
		}

		public static System.IO.Stream CreateFileOutputStream(String filePath)
		{
			#if ANDROID
			if (filePath.IsSmbFile())
			{
				return SambaHelpers.GetSambaOutputStream(filePath);
			}
			#endif
			return System.IO.File.Create(filePath);
		}

		public static bool FileExists(String filePath)
		{
			#if ANDROID
			if (filePath.IsSmbFile())
			{
				return SambaHelpers.FileExists(filePath);
			}
			#endif
			return System.IO.File.Exists(filePath);
		}

		public static String AppendFileName(String dir, String name)
		{
			#if ANDROID
			if (dir.IsSmbFile())
			{
				return SambaHelpers.AppendFileName(dir, name);
			}
			#endif
			name = name.Replace('/', System.IO.Path.DirectorySeparatorChar);
			return System.IO.Path.Combine(dir, name);
		}


		public static String GetDirectory(String filePath)
		{
			#if ANDROID
			if (filePath.IsSmbFile())
			{
				return SambaHelpers.GetDirectoryName(filePath);
			}
			#endif
			return System.IO.Path.GetDirectoryName(filePath);
		}

		public static void CreateDirectory(String dirPath)
		{
			#if ANDROID
			if (dirPath.IsSmbFile())
			{
				SambaHelpers.CreateDirectory(dirPath);
				return;
			}
			#endif
			System.IO.Directory.CreateDirectory(dirPath);
		}
	}

    class ProjectManager : IProjectManager
    {
        #region IProjectManager Members

        public IProject CreateProject(String title)
        {
            return new Project(title);
        }

        public IProject LoadProject(String fileName)
        {
            IList<IXmlWritable> elements = DoImportElements(fileName, true);
            if (elements.Count > 0 && elements[0] is IProject)
            {
                return (IProject)elements[0];
            }
            else
            {
                throw new InvalidProjectException();
            }
        }

        public void SaveProject(IProject project)
        {
            if (String.IsNullOrEmpty(project.FileName))
            {
                throw new ArgumentException(StringResources.FileNameMustBeSet);
            }
            DoSaveProject(project, project.FileName);
            project.Changed = false;
        }

        public void SaveProject(IProject project, String fileName)
        {
            DoSaveProject(project, fileName);
            project.FileName = fileName;
            project.Changed = false;
        }

        private void DoSaveProject(IProject project, String fileName)
        {
            List<IXmlWritable> elements = new List<IXmlWritable>();
            elements.Add(project);
            DoExportElements(elements, fileName, true);
        }

        public void UnloadProject(IProject project)
        {
            DataModule.TheElementRepository.Clear();
        }

        public void ExportElements(IList<IXmlWritable> elements, String fileName)
        {
            DoExportElements(elements, fileName, false);
        }

        public void ExportElements(IList<IXmlWritable> elements, System.Text.StringBuilder output)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = true;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                DoExportElements(elements, writer, false);
            }
        }

        private void DoExportElements(IList<IXmlWritable> elements, String fileName, bool isProject)
        {
            String tempFileName = System.IO.Path.GetTempFileName();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = true;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(tempFileName, settings))
            {
                DoExportElements(elements, writer, isProject);
            }
            System.IO.File.Copy(tempFileName, fileName, true);
            System.IO.File.Delete(tempFileName);
        }

        private void DoExportElements(IList<IXmlWritable> elements, XmlWriter writer, bool isProject)
        {
            writer.WriteStartDocument();
            if (!isProject)
            {
                writer.WriteStartElement("ExportedElements");
            }
            foreach (IXmlWritable element in elements)
            {
                element.WriteToXml(writer);
                if (element is IBackgroundSoundChoice)
                {
                    BackgroundSounds.WriteAdditionalData(writer, element as IBackgroundSoundChoice);
                }
            }
            if (!isProject)
            {
                writer.WriteEndElement();
            }
            writer.WriteEndDocument();
            writer.Flush();
        }

        public IList<IXmlWritable> ImportElements(String fileName)
        {
            return DoImportElements(fileName, false);
        }

        private IList<IXmlWritable> DoImportElements(String fileName, bool isProject)
        {
            List<IXmlWritable> elements = new List<IXmlWritable>();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.DtdProcessing = DtdProcessing.Ignore;
			using (System.IO.Stream stream = FileHelpers.GetFileContentStream(fileName))
            {
				using (System.IO.StreamReader streamReader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8)) 
				{
	                using (XmlReader reader = XmlReader.Create(streamReader, settings))
	                {
	                    DoImportElements(reader, elements, fileName, isProject);
	                }
				}
            }

            return elements;
        }

        public IList<IXmlWritable> ImportElementsFromString(String serializedData)
        {
            List<IXmlWritable> elements = new List<IXmlWritable>();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.DtdProcessing = DtdProcessing.Ignore;
            using (System.IO.StringReader reader = new System.IO.StringReader(serializedData))
            {
                using (XmlReader xmlReader = XmlReader.Create(reader, settings))
                {
                    DoImportElements(xmlReader, elements, String.Empty, false);
                }
            }
            return elements;
        }

        private void DoImportElements(XmlReader reader, List<IXmlWritable> elements, String fileName, bool isProject)
        {
            reader.Read();
            reader.MoveToElement();
            if (!reader.IsStartElement("ExportedElements") && !reader.IsStartElement("Project"))
            {
                XmlHelpers.ThrowException(String.Format(StringResources.ExpectedElement, "ExportedElements"), reader);
            }
            if (reader.IsEmptyElement)
            {
                reader.Read();
            }
            else
            {
                DataModule.TheElementRepository.Redirector = new ReferenceRedirector();
                if (!isProject)
                {
                    reader.Read(); // ExportedElements or Project
                    while (reader.IsStartElement())
                    {
                        if (reader.IsStartElement("Modes")) // in project files
                        {
                            if (!reader.IsEmptyElement)
                            {
                                reader.Read();
                                ReadElements(elements, reader, fileName);
                                reader.ReadEndElement();
                            }
                            else
                                reader.Read();
                        }
                        else
                        {
                            ReadElements(elements, reader, fileName);
                        }
                    }
                    reader.ReadEndElement();
                }
                else
                {
                    ReadElements(elements, reader, fileName);
                }
            }
        }

        private void ReadElements(IList<IXmlWritable> elements, XmlReader reader, String fileName)
        {
            while (reader.IsStartElement())
            {
                IXmlWritable element = ReadElement(reader, fileName);
                if (element != null)
                {
                    elements.Add(element);
                }
            }
        }

        private IXmlWritable ReadElement(XmlReader reader, String fileName)
        {
            if (reader.IsStartElement("Project"))
            {
                return new Project(reader, fileName);
            }
            else if (reader.IsStartElement("Mode"))
            {
                return new Mode(reader);
            }
            else if (reader.IsStartElement("ModeElement"))
            {
                return new ModeElement(reader);
            }
            else
            {
                return DataModule.TheElementFactory.CreateElement(reader);
            }
        }

        #endregion
    }
}
