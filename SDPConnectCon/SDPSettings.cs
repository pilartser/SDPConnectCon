using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SDPConnectCon
{
    [XmlRoot(ElementName = "settings")]
    public struct SdpSettings
    {
        private string _pathLog;
        private string _pathRegistry;
        private string _pathErrorRegistry;
        private string _version;
        private string _versionProtocol;


        /// <summary>
        /// Версия файла настроек
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public string Version {
            get { return _version ?? "1.0"; }
            set { _version = value; }
        }

        /// <summary>
        /// Путь, по которому ищется лог
        /// </summary>
        [DataMember(IsRequired = true)]
        [XmlElement(ElementName = "pathLog", Order = 1)]
        public string PathLog {
            get { return _pathLog; }
            set
            {
                if (value == null) throw new Exception("Директория лога должна быть задана");
                if (!Directory.Exists(value)) throw new Exception("Не найдена заданная в настройках директория лога");
                _pathLog = value;
            }
        }

        /// <summary>
        /// Путь, по которому ищется реестр строк
        /// </summary>
        [XmlElement(ElementName = "pathRegistry", Order = 2)]
        public string PathRegistry {
            get { return _pathRegistry; }
            set { if (!Directory.Exists(value)) throw new Exception("Не найдена заданная в настройках директория реестра строк");
                _pathRegistry = value;
            }
        }

        /// <summary>
        /// Путь, по которому ищется строк, отбракованных сервисом
        /// </summary>
        [XmlElement(ElementName = "pathErrorRegistry", Order = 3)]
        public string PathErrorRegistry
        {
            get { return _pathErrorRegistry; }
            set
            {
                if (!Directory.Exists(value)) throw new Exception("Не найдена заданная в настройках директория реестра строк, отбракованных сервисом");
                _pathErrorRegistry = value;
            }
        }

        /// <summary>
        /// Строковое значение кода агента
        /// </summary>
        [XmlElement(ElementName = "agentId", IsNullable = false, Order = 4)]
        public string AgentId { get; set; }

        /// <summary>
        /// Номер точки продажи
        /// </summary>
        [XmlElement(ElementName = "salepointId", IsNullable = false, Order = 5)]
        public string SalepointId { get; set; }

        /// <summary>
        /// Номер региона, для которого подключена карта
        /// </summary>
        [XmlElement(ElementName = "regionId", IsNullable = false, Order = 6)]
        public int RegionId { get ; set; }

        /// <summary>
        /// Идентификатор устройства, выполняющего операцию
        /// </summary>
        [XmlElement(ElementName = "deviceId", IsNullable = true, Order = 7)]
        public string DeviceId { get; set; }

        /// <summary>
        /// Идентификатор версии протокола
        /// </summary>
        [XmlElement(ElementName = "versionProtocol", IsNullable = true, Order = 8)]
        public string VersionProtocol {
            get { return _versionProtocol ?? "1"; }
            set { _versionProtocol = value; }
        }
    }
}
