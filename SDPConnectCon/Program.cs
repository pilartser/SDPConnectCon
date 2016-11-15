using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SDPConnect;
using SDPConnectCon.SDPService;

namespace SDPConnectCon
{
    internal class Program
    {
        private static Row[] _rows;

        private static void Main(string[] args)
        {
            //ServiceDoSmth();
            LoadReestr("Test.txt");
            Console.ReadLine();
        }

        private static void LoadReestr(string path)
        {
            try
            {
                WriteLog("Начинается загрузка файла...");

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

                var brokenLines = payLines.Where(row => row.Value.Split(Row.Separator).Length != 15).ToList();
                if (brokenLines.Any())
                {
                    foreach (var line in brokenLines)
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
                WriteLog("Начало работы с сервисом...");
                foreach (var row in _rows)
                {
                    ServiceCall(row);
                }
                WriteLog("Загрузка файла успешно завершена");
            }
            catch (Exception e)
            {
                _rows = null;
                var errorText = $"При загрузке данных из текстового файла произошла следующая ошибка: \r\n**********\r\n{e.Message}\r\n**********";
                WriteLog(errorText);
            }
        }

        private static void ServiceCall(Row row)
        {
            try
            {
                var str = typeof(Row).GetProperties().Aggregate("{", (current, prop) => current + $"{prop.Name} = {prop.GetValue(row, null)}; ");
                WriteLog($"{str}}}");
                var sdp = new SdpServiceClient ();

                var requestCardInfo = new CardInfoRequest
                {
                    deviceId = "99999",
                    regionId = 99,
                    sysNum = 6060,
                    agentId = "1",
                    salepointId = "1",
                    version = "1"
                };

                var cardInfoResponse = sdp.CardInfo(requestCardInfo);
                foreach (var warning in cardInfoResponse.CardInformation.warningMsg)
                {
                    WriteLog(warning);
                }

                var requestCardPayment = new CardPaymentRequest
                {
                    agentId = "1",
                    paymentInfo = "Какой-то платеж",
                    paymentSum = 200000,
                    salepointId = "1",
                    sessionId = cardInfoResponse.CardInformation.sessionId,
                    tariffId = "12",
                    version = "1"
                };
                WriteLog(cardInfoResponse.CardInformation.tariff.text);
                //var cardPaymentResponse = sdp.CardPayment(requestCardPayment);
                //WriteLog(cardPaymentResponse.CardPaymentInformation.fullSum.ToString());
            }
            catch (Exception e)
            {
                WriteLog(e.Message);
                if (e.InnerException != null)
                    WriteLog(e.InnerException.Message);
            }
        }

        private static void WriteLog(String str)
        {
            Console.WriteLine(str);
        }
    }
}
