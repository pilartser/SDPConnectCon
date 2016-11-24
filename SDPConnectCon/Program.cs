using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using SDPConnectCon.SDPService;

namespace SDPConnectCon
{
    internal class Program
    {
        private static Row[] _rows;
        private static string _settingsName = "settings.xml";
        private static SdpSettings _settings;
        private static string _logPath;

        private static int Main(string[] args)
        {
            _logPath = $"logSdpConnectCon{DateTime.Now.ToString("ddMMyyyy_HHmmss")}.log";
            try
            {
                
                WriteLog($"Открываем лог {_logPath}");
                WriteLog($"Номер версии исполняемого файла {Assembly.GetExecutingAssembly().GetName().Version}");
                WriteLog($"Номер версии файла настроек {_settings.Version}");
                if (args.Length != 1)
                    throw new Exception("В приложение ожидается передача только одного параметра.");
                WriteLog("Получаем файл настроек...");
                _settings = (SdpSettings) DeSerializeObject(typeof (SdpSettings), _settingsName);
                var fullPath = Path.Combine(_settings.Path, args[0]);
                if (!File.Exists(fullPath))
                    throw new Exception($"Не найден файл {fullPath}.");
                LoadReestr(fullPath);
                WriteLog("Закрываем лог");
                return 0;
            }
            catch (Exception e)
            {
                Environment.ExitCode = 1;
                if (!File.Exists(_logPath))
                {
                    Console.WriteLine("Файл лога не смог быть создан.");
                    Console.ReadLine();
                }
                else 
                    WriteLog($"Выполнение программы завершается ошибкой: \r\n{e.Message}");
            }
            return 666;
        }

        private static void LoadReestr(string path)
        {
            try
            {
                WriteLog("Начинается загрузка реестра...\r\n");

                WriteLog($"Открыт файл: {path}.");
                _rows = null;
                var lines = File.ReadAllLines(path, Encoding.GetEncoding(1251));
                if (lines.Length == 0) throw new Exception("В реестре не обнаружено ни одной строки.");
                if ((lines.Length < 3) || (lines[lines.Length - 2] != "="))
                    throw new Exception("В реестре не обнаружен признак контрольной строки.");
                var controlLine = lines[lines.Length - 1].Split(Row.Separator).ToArray();
                if (controlLine.Length != 6) throw new Exception("В контрольной строке не 6 полей.");
                var payLines = lines.Select((a, i) => new {Value = a, Index = i + 1})
                    .Where(row => (row.Index < lines.Length - 1)).ToList();

                var badFormatLines = payLines.Where(row => row.Value.Split(Row.Separator).Length != 15).ToList();
                if (badFormatLines.Any())
                {
                    foreach (var line in badFormatLines)
                    {
                        WriteLog($"Строка {line.Index}, содержит не 15 полей: \"{line.Value}\".");
                    }
                    throw new Exception(
                        $"При загрузке данных найдены строки, содержащие не 15 полей.");
                }
                _rows = payLines.Select(p => new Row(p.Index, p.Value)).ToArray();
                if (!Row.CompareRows(_rows, controlLine))
                    throw new Exception("Контрольная строка не совпадает с загруженными данными.");
                WriteLog("Контрольная строка совпадает с загруженными данными.");
                WriteLog("Начало работы с сервисом...\r\n");
                var badThreatingByServiceLines = _rows.Where(p => !ServiceCall(p)).ToArray();
                if (badThreatingByServiceLines.Length > 0)
                {
                    var str = badThreatingByServiceLines.Aggregate("",
                        (current, p) => current + p.Index + ((p.Index != badThreatingByServiceLines.Length) ? "," : ""));
                    var errorReestrPath = Path.Combine(_settings.Path,
                        $"ErrorSB_{Path.GetFileNameWithoutExtension(path)}_{DateTime.Now.ToString("ddMMyyyy_HHmmss")}.txt");
                    Row.GenerateErrorReestr(errorReestrPath, lines, badThreatingByServiceLines);
                    WriteLog(
                        $"Для строк {str} сгенерирован реестр ошибочных строк, пригодный для повторной загрузки {errorReestrPath}");
                    throw new Exception($"Некоторые строки не были обработаны сервисом.");
                }
                WriteLog("Загрузка реестра успешно завершена");
            }
            catch (Exception e)
            {
                _rows = null;
                throw new Exception($"При загрузке реестра произошла следующая ошибка: {e.Message}");
            }
        }

        private static bool ServiceCall(Row row)
        {
            try
            {
                var sdp = new SdpServiceClient();
                

                WriteLog($"Распознанная строка реестра: {row}}}");

                var requestCardInfo = new CardInfoRequest
                {
                    version = _settings.VersionProtocol,
                    agentId = _settings.AgentId,
                    salepointId = _settings.SalepointId,
                    sysNum = row.CardNumber,
                    regionId = _settings.RegionId,
                    deviceId = _settings.DeviceId
                };
                
                var requestProps = typeof (CardInfoRequest).GetProperties()
                    .Aggregate("{",
                        (current, prop) => current + $"{prop.Name} = {prop.GetValue(requestCardInfo, null)}; ");
                WriteLog($"Заполненный данными перед отправкой CardInfoRequest: {requestProps}}}");

                var cardInfoResponse = sdp.CardInfo(requestCardInfo);
                if (cardInfoResponse == null) throw new Exception("Получен пустой CardInfoResponse");
                if (cardInfoResponse.Result.resultCode != 0)
                    throw new Exception(
                        $"CardInfoResponse вернул следующую ошибку: {cardInfoResponse.Result.resultCodeText}");
                if (cardInfoResponse.CardInformation.warningMsg != null)
                {
                    WriteLog($"CardInfoResponse содержит следующие предупреждения:");
                    foreach (var warning in cardInfoResponse.CardInformation.warningMsg)
                    {
                        WriteLog(warning);
                    }
                }
                if (cardInfoResponse.CardInformation.tariff == null)
                    throw new Exception("CardInfoResponse не содержит ни одного тарифа.");
                int paymentSumKopecks = row.GetPaymentSumKopecks();
                if (paymentSumKopecks < cardInfoResponse.CardInformation.tariff.minSumInt)
                    throw  new Exception($"Сумма пополнения {paymentSumKopecks} меньше минимально разрешенной {cardInfoResponse.CardInformation.tariff.minSumInt}");
                if (paymentSumKopecks > cardInfoResponse.CardInformation.tariff.maxSumInt)
                    throw new Exception($"Сумма пополнения {paymentSumKopecks} больше максимально разрешенной {cardInfoResponse.CardInformation.tariff.maxSumInt}");
                WriteLog("Успешно получен CardInfoResponse");
                var requestCardPayment = new CardPaymentRequest
                {
                    version = _settings.VersionProtocol,
                    agentId = _settings.AgentId,
                    salepointId = _settings.SalepointId,
                    sessionId = cardInfoResponse.CardInformation.sessionId,
                    tariffId = cardInfoResponse.CardInformation.tariff.id,
                    paymentSum = paymentSumKopecks,
                    paymentInfo = $"{row.BranchNo}_{row.CashierNo}"
                };
                requestProps = typeof (CardPaymentRequest).GetProperties()
                    .Aggregate("{",
                        (current, prop) => current + $"{prop.Name} = {prop.GetValue(requestCardPayment, null)}; ");
                WriteLog($"Заполненный данными перед отправкой CardPaymentRequest: {requestProps}}}");
                var cardPaymentResponse = sdp.CardPayment(requestCardPayment);
                if (cardPaymentResponse == null) throw new Exception("Получен пустой CardPaymentResponse");
                if (cardPaymentResponse.Result.resultCode != 0)
                    throw new Exception(
                        $"CardPaymaentResponse вернул следующую ошибку: {cardPaymentResponse.Result.resultCodeText ?? ""}");
                WriteLog($"Получен следующий чек:\r\n{cardPaymentResponse.CardPaymentInformation.cheq ?? ""}");
                return true;
            }
            catch (Exception e)
            {
                WriteLog($"Ошибка общения с сервисом: {e.Message} {e.InnerException?.Message ?? ""}");
                WriteLog("\r\n");
            }
            return false;
        }

        public static object DeSerializeObject(Type type, string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    XmlSerializer xs = new XmlSerializer(type);
                    return xs.Deserialize(fs);
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            throw new Exception($"Ошибка получения содержимого файла {path}");
        }

        public static void WriteSerializedObject(Type type, object serialized, string path,
            XmlSerializerNamespaces xmlns = null)
        {
            try
            {
                var xs = new XmlSerializer(type);
                using (var fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    StreamWriter myStreamWriter = new StreamWriter(fs);
                    if (xmlns != null)
                        xs.Serialize(myStreamWriter, serialized, xmlns);
                    else
                        xs.Serialize(myStreamWriter, serialized);
                    WriteLog($"File {path} successfully created!");
                }
            }
            catch (Exception e)
            {
                WriteLog($"Error: {e.Message}");
            }
        }

        private static void WriteLog(string str)
        {
            Console.WriteLine(str);
            try
            {
                using (var sr = File.AppendText(_logPath))
                {
                    sr.WriteLine(str);
                }
            }
            catch (Exception)
            {
                throw new Exception($"Ошибка записи в файл {_logPath}");
            }
        }
    }
}
