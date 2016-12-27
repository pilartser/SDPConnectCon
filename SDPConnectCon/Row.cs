using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SDPConnectCon
{
    public class Row
    {
        internal const char Separator = ';';
        private static readonly NumberFormatInfo DigitalComma = new NumberFormatInfo { NumberDecimalSeparator = "," };
        private static readonly NumberFormatInfo DigitalDot = new NumberFormatInfo { NumberDecimalSeparator = "."};

        private readonly DateTime _date;

        private readonly int _cardNumber;
        private readonly decimal _paymentSum;
        private readonly decimal _amount;
        private readonly decimal _transferSum;
        private readonly decimal _comissionSum;

        /// <summary>
        /// Номер строки в реестре (начинается с 1)
        /// </summary>
        public int Index { get; }


        /// <summary>
        /// Дата и время платежа
        /// </summary>
        public DateTime Date => _date;

        /// <summary>
        /// Номер отделения
        /// </summary>
        public string BranchNo { get; }

        /// <summary>
        /// Номер кассира/УС/СБОЛ
        /// </summary>
        public string CashierNo { get;}
        
        /// <summary>
        /// Уникальный код операции в ЕПС
        /// </summary>
        public string Id { get;}

        /// <summary>
        /// Номер карты
        /// </summary>
        public int CardNumber => _cardNumber;

        /// <summary>
        /// Сумма по услуге
        /// </summary>
        public decimal PaymentSum => _paymentSum;

        /// <summary>
        /// Общая сумма платежа
        /// </summary>
        public decimal Amount => _amount;

        /// <summary>
        /// Общая сумма перевода
        /// </summary>
        public decimal TransferSum => _transferSum;

        /// <summary>
        /// Сумма комиссии банку от общей суммы
        /// </summary>
        public decimal CommissionSum => _comissionSum;

        public Row(int index, string str)
        {
            if (str.Split(Separator).Length != 15)
                throw new Exception($"Строка с номером {index} имеет не 15 полей: {str}");
            var row =
                new [] {str}.Select(p => p.Split(Separator))
                    .Select(p => new
                    {
                        Date = $"{p[0]} {p[1]}",
                        BranchNo = p[2] ?? "",
                        CashierNo = p[3] ?? "",
                        Id = p[4] ?? "",
                        CardNumber = ConvertAccountToCardNumber(p[5]),
                        PaymentSum = p[9],
                        Amount = p[12],
                        TransferSum = p[13],
                        CommissionSum = p[14]
                    }).First();
            
            if (
                !DateTime.TryParseExact(row.Date, "dd-MM-yyyy HH-mm-ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _date)) throw new Exception($"Ошибка преобразования даты платежа {row.Date}");
            if (!int.TryParse(row.CardNumber, out _cardNumber))
                throw new Exception($"Ошибка приведения номера карты к integer {row.CardNumber}");
            if (
                !decimal.TryParse(row.PaymentSum, NumberStyles.AllowDecimalPoint,
                    DigitalDot, out _paymentSum))
                throw new Exception($"Ошибка приведения суммы по услуге к decimal {row.PaymentSum}");
            if (
                !decimal.TryParse(row.Amount, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _amount))
                throw new Exception($"Ошибка приведения общей суммы платежа к decimal {row.Amount}");
            if (
                !decimal.TryParse(row.TransferSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _transferSum))
                throw new Exception($"Ошибка приведения общей суммы перевода к decimal {row.TransferSum}");
            if (
                !decimal.TryParse(row.CommissionSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _comissionSum))
                throw new Exception(
                    $"Ошибка приведения суммы комиссии банка от общей суммы к decimal {row.CommissionSum}");

            Index = index;
            Id = row.Id;
            BranchNo = row.BranchNo;
            CashierNo = row.CashierNo;
        }

        public int GetPaymentSumKopecks()
        {
            return (int) (_paymentSum*100);
        }

        public override string ToString()
        {
            return GetType().GetProperties()
                    .Aggregate("{",
                        (current, prop) => current + $"{prop.Name} = {prop.GetValue(this, null)}; ");
        }

        private string ConvertAccountToCardNumber(string account)
        {
            if (account.Length <= 4) throw new ArgumentException("Длина счета должна быть больше 4 символов");
            return $"000{account.Substring(3, account.Length-4)}";
        }

        internal static bool CompareRows(Row[] rows, string[] line)
        {
            var controlLine = new[]{line}.Select(p => new {TotalCount = p[0], TotalAmount = p[1], TotalTransferSum = p[2], TotalCommissionSum = p[3]}).First();
            int totalCount;
            decimal totalAmount;
            decimal totalTransferSum;
            decimal totalCommissionSum;

            if (!int.TryParse(controlLine.TotalCount, out totalCount)) throw new Exception($"Ошибка приведения количества строк в реестре к int {controlLine.TotalCount}");
            if (
                !decimal.TryParse(controlLine.TotalAmount, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalAmount))
                throw new Exception($"Ошибка приведения общей суммы принятых средств к decimal {controlLine.TotalAmount}");
            if (
                !decimal.TryParse(controlLine.TotalTransferSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalTransferSum))
                throw new Exception($"Ошибка приведения общей суммы перечисления клиенту к decimal {controlLine.TotalTransferSum}");
            if (
                !decimal.TryParse(controlLine.TotalCommissionSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalCommissionSum))
                throw new Exception($"Ошибка приведения общей комиссии банку к decimal {controlLine.TotalCommissionSum}");

            var total =  new
            {
                TotalAmount = rows.Sum(p => p.Amount),
                TotalTransferSum = rows.Sum(p => p.TransferSum),
                TotalCommissionSum = rows.Sum(p => p.CommissionSum)
            };

            return ((totalCount == rows.Length) && (totalAmount == total.TotalAmount) && (totalTransferSum == total.TotalTransferSum) && (totalCommissionSum == total.TotalCommissionSum));
        }

        internal static void GenerateErrorReestr(string path, string[] lines, Row[] brokenRows)
        {
            try
            {
                using (var fs = File.CreateText(path))
                {
                    foreach (var row in brokenRows)
                    {
                        fs.WriteLine(lines[row.Index - 1]);
                    }
                    var total = new
                    {
                        TotalAmount = brokenRows.Sum(p => p.Amount),
                        TotalTransferSum = brokenRows.Sum(p => p.TransferSum),
                        TotalCommissionSum = brokenRows.Sum(p => p.CommissionSum)
                    };
                    fs.WriteLine(
                        $"={brokenRows.Length}{Separator}{total.TotalAmount}{Separator}{total.TotalTransferSum}{Separator}{total.TotalCommissionSum}{Separator}{Separator}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка формирования реестра ошибочных строк {path}: {e.Message}");
            }
        }

    }
}
