using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace SDPConnectCon
{
    [XmlRoot(ElementName = "settings")]
    public struct SdpSettings : IXmlSerializable
    {
        private string _version;
        private string _versionProtocol;

        /// <summary>
        /// Версия файла настроек
        /// </summary>
        public string Version
        {
            get { return _version ?? "0.0"; }
            set
            {
                if (value == null)
                    throw new Exception("Не указана версия файла настроек");
                _version = value;
            }
        }

        public Path Pathes { get; set; }

        /// <summary>
        /// Строковое значение кода агента
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// Номер точки продажи
        /// </summary>
        public string SalepointId { get; set; }

        /// <summary>
        /// Номер региона, для которого подключена карта
        /// </summary>
        public int RegionId { get; set; }

        /// <summary>
        /// Идентификатор устройства, выполняющего операцию
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Идентификатор версии протокола
        /// </summary>
        public string VersionProtocol
        {
            get { return _versionProtocol ?? "0"; }
            set { _versionProtocol = value; }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            Version = reader["version"];
            reader.ReadStartElement("settings");
            reader.ReadStartElement("path");
            Pathes = new Path
            {
                Log = reader.ReadElementContentAsString("log", ""),
                Registry = reader.ReadElementContentAsString("registry", ""),
                ErrorRegistry = reader.ReadElementContentAsString("errorRegistry", "")
            };
            if (!reader.IsEmptyElement) reader.ReadEndElement();
            AgentId = reader.ReadElementContentAsString("agentId", "");
            SalepointId = reader.ReadElementContentAsString("salepointId", "");
            RegionId = reader.ReadElementContentAsInt("regionId", "");
            DeviceId = reader.ReadElementContentAsString("deviceId", "");
            VersionProtocol = reader.ReadElementContentAsString("versionProtocol", "");
            if (!reader.IsEmptyElement) reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException("Реализация сериализации файла настроек не предполагается");
        }
    }

    [XmlRoot(ElementName = "path")]
    public struct Path
    {
        private string _log;
        private string _registry;
        private string _errorRegistry;

        /// <summary>
        /// Путь, по которому ищется лог
        /// </summary>
        [DataMember(Name = "log", IsRequired = true), XmlElement(ElementName = "log", Order = 1)]
        public string Log
        {
            get { return _log; }
            set
            {
                if (!Directory.Exists(value)) throw new Exception($"Не найдена заданная в настройках директория лога {value}");
                _log = value;
            }
        }

        /// <summary>
        /// Путь, по которому ищется реестр строк
        /// </summary>
        [XmlElement(ElementName = "registry", Order = 2)]
        public string Registry
        {
            get { return _registry; }
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception($"Не найдена заданная в настройках директория реестра строк {value}");
                _registry = value;
            }
        }

        /// <summary>
        /// Путь, по которому ищется строк, отбракованных сервисом
        /// </summary>
        [XmlElement(ElementName = "errorRegistry", Order = 3)]
        public string ErrorRegistry
        {
            get { return _errorRegistry; }
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(
                        $"Не найдена заданная в настройках директория реестра строк, отбракованных сервисом {value}");
                _errorRegistry = value;
            }
        }
    }
}
