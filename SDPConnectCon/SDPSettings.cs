using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SDPConnectCon
{
    [XmlRoot(ElementName = "settings")]
    public struct SdpSettings
    {
        private string _path;
        private string _version;

        /// <summary>
        /// Путь, по которому ищется файл .txt и куда пишется лог
        /// </summary>
        [XmlElement(ElementName = "path")]
        public string Path {
            get { return _path; }
            set { if (!Directory.Exists(value)) throw new Exception("Не найдена заданная в настройках директория");
                _path = value;
            }
        }

        /// <summary>
        /// Строковое значение кода агента
        /// </summary>
        [XmlElement(ElementName = "agentId", IsNullable = false)]
        public string AgentId { get; set; }

        /// <summary>
        /// Номер точки продажи
        /// </summary>
        [XmlElement(ElementName = "salepointId", IsNullable = false)]
        public string SalepointId { get; set; }

        /// <summary>
        /// Номер региона, для которого подключена карта
        /// </summary>
        [XmlElement(ElementName = "regionId", IsNullable = false)]
        public int RegionId { get ; set; }

        /// <summary>
        /// Идентификатор устройства, выполняющего операцию
        /// </summary>
        [XmlElement(ElementName = "deviceId", IsNullable = true)]
        public string DeviceId { get; set; }

        /// <summary>
        /// Идентификатор версии протокола
        /// </summary>
        [XmlElement(ElementName = "version", IsNullable = true)]
        public string Version {
            get { return _version ?? "1"; }
            set { _version = value; }
        }
    }
}
