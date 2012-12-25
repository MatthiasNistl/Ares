﻿/*
 Copyright (c) 2010 [Joerg Ruedenauer]
 
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

namespace Ares.Data
{
    [Serializable]
    class Project : IProject
    {
        public String Title { get; set; }

        public String FileName 
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        public bool Changed 
        {
            get { return m_Changed; }
            set { m_Changed = value; }
        }

        public int TagLanguageId { get; set; }

        public IMode AddMode(String title)
        {
            Mode mode = new Mode(title);
            m_Modes.Add(mode);
            return mode;
        }

        public void InsertMode(int index, IMode mode)
        {
            m_Modes.Insert(index, mode);
        }

        public void RemoveMode(IMode mode)
        {
            m_Modes.Remove(mode);
        }

        public IList<IMode> GetModes()
        {
            return new List<IMode>(m_Modes);
        }

        public bool ContainsKeyMode(Int32 keyCode)
        {
            return GetMode(keyCode) != null;
        }

        public IMode GetMode(Int32 keyCode)
        {
            return m_Modes.Find(m => m.KeyCode == keyCode);
        }

        public void WriteToXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("Project");
            writer.WriteAttributeString("Title", Title);
            writer.WriteAttributeString("LanguageId", TagLanguageId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteStartElement("Modes");
            m_Modes.ForEach(e => e.WriteToXml(writer));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        internal Project(String title)
        {
            Title = title;
            m_Modes = new List<IMode>();
            FileName = "";
            TagLanguageId = -1;
            Changed = true;
        }

        internal Project(System.Xml.XmlReader reader, String fileName)
        {
            m_Modes = new List<IMode>();

            if (!reader.IsStartElement("Project"))
            {
                XmlHelpers.ThrowException(String.Format(StringResources.ExpectedElement, "Project"), reader);
            }
            Title = reader.GetNonEmptyAttribute("Title");
            TagLanguageId = reader.GetIntegerAttributeOrDefault("LanguageId", -1);
            if (!reader.IsEmptyElement)
            {
                reader.Read();
                while (reader.IsStartElement())
                {
                    if (reader.IsStartElement("Modes") && !reader.IsEmptyElement)
                    {
                        reader.Read();
                        while (reader.IsStartElement())
                        {
                            if (reader.IsStartElement("Mode"))
                            {
                                m_Modes.Add(new Mode(reader));
                            }
                            else
                            {
                                reader.ReadOuterXml();
                            }
                        }
                        reader.ReadEndElement();
                    }
                    else
                    {
                        reader.ReadOuterXml();
                    }
                }
                reader.ReadEndElement();
            }
            else
            {
                reader.Read();
            }
            FileName = fileName;
            Changed = false;
        }

        private List<IMode> m_Modes;

        [NonSerialized]
        private String m_FileName;
        [NonSerialized]
        private bool m_Changed;

    }
}
