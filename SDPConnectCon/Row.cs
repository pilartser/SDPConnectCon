using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SDPConnect
{
    internal enum RowStatus
    {
        Added,
        Treated,
        Faulted,
        Finished
    }

    public class Row
    {
        internal const char Separator = ';';
        private static readonly NumberFormatInfo DigitalComma = new NumberFormatInfo { NumberDecimalSeparator = "," };
        private DateTime _date;

        private int _cardNumber;
        private float _paymentSum;
        private float _amount;
        private float _transferSum;
        private float _comissionSum;

        /// <summary>
        /// Номер строки в реестре (начинается с 1)
        /// </summary>
        public int Index { get; set; }


        /// <summary>
        /// Дата и время платежа
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }

        /// <summary>
        /// Уникальный код операции в ЕПС
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Номер карты
        /// </summary>
        public int CardNumber
        {
            get { return _cardNumber; }
            set { _cardNumber = value; }
        }

        /// <summary>
        /// Сумма по услуге
        /// </summary>
        public float PaymentSum
        {
            get { return _paymentSum; }
            set { _paymentSum = value; }
        }

        /// <summary>
        /// Общая сумма платежа
        /// </summary>
        public float Amount
        {
            get { return _amount; }
            set { _amount = value; }
        }

        /// <summary>
        /// Общая сумма перевода
        /// </summary>
        public float TransferSum
        {
            get { return _transferSum; }
            set { _transferSum = value; }
        }

        /// <summary>
        /// Сумма комиссии банку от общей суммы
        /// </summary>
        public float CommissionSum
        {
            get { return _comissionSum; }
            set { _comissionSum = value; }
        }

        public Row(int index, string str)
        {
            if (str.Split(Separator).Length != 15)
                throw new Exception($"Строка с номером {index} имеет не 15 полей: {str}");
            var row =
                new [] {str}.Select(p => p.Split(Separator))
                    .Select(p => new
                    {
                        Date = $"{p[0]} {p[1]}",
                        Id = Id = p[4],
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
                !float.TryParse(row.PaymentSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _paymentSum))
                throw new Exception($"Ошибка приведения суммы по услуге к float {row.PaymentSum}");
            if (
                !float.TryParse(row.Amount, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _amount))
                throw new Exception($"Ошибка приведения общей суммы платежа к float {row.Amount}");
            if (
                !float.TryParse(row.TransferSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _transferSum))
                throw new Exception($"Ошибка приведения общей суммы перевода к float {row.TransferSum}");
            if (
                !float.TryParse(row.CommissionSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out _comissionSum))
                throw new Exception(
                    $"Ошибка приведения суммы комиссии банка от общей суммы к float {row.CommissionSum}");
            Index = index;
        }

        private string ConvertAccountToCardNumber(string account)
        {
            if (account.Length <= 4) throw new ArgumentException("Длина счета должна быть больше 4 символов");
            return $"000{account.Substring(3, account.Length-4)}";
        }

        private void AddTo(DataTable dt)
        {
            var row = dt.Rows.Add();
            foreach (var prop in typeof(Row).GetProperties())
            {
                row[$"col{prop.Name}"] = typeof(Row).GetProperty(prop.Name).GetValue(this, null);
            }
            row["colStatus"] = (int)RowStatus.Added;
        }

        

        internal static DataTable PrepareDataTable()
        {
            DataTable dt = new DataTable();
            foreach (var prop in typeof(Row).GetProperties())
            {
                dt.Columns.Add(new DataColumn($"col{prop.Name}"));
            }
            dt.Columns.Add(new DataColumn("colStatus"));
            return dt;
        }

        internal static bool CompareRows(Row[] rows, string[] line)
        {
            var controlLine = new[]{line}.Select(p => new {TotalCount = p[0], TotalAmount = p[1], TotalTransferSum = p[2], TotalCommissionSum = p[3]}).First();
            int totalCount;
            float totalAmount;
            float totalTransferSum;
            float totalCommissionSum;

            if (!int.TryParse(controlLine.TotalCount, out totalCount)) throw new Exception($"Ошибка приведения количества строк в реестре к int {controlLine.TotalCount}");
            if (
                !float.TryParse(controlLine.TotalAmount, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalAmount))
                throw new Exception($"Ошибка приведения общей суммы принятых средств к float {controlLine.TotalAmount}");
            if (
                !float.TryParse(controlLine.TotalTransferSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalTransferSum))
                throw new Exception($"Ошибка приведения общей суммы перечисления клиенту к float {controlLine.TotalTransferSum}");
            if (
                !float.TryParse(controlLine.TotalCommissionSum, NumberStyles.AllowDecimalPoint,
                    DigitalComma, out totalCommissionSum))
                throw new Exception($"Ошибка приведения общей комиссии банку к float {controlLine.TotalCommissionSum}");

            var total =  new
            {
                TotalAmount = rows.Sum(p => p.Amount),
                TotalTransferSum = rows.Sum(p => p.TransferSum),
                TotalCommissionSum = rows.Sum(p => p.CommissionSum)
            };

            return ((totalCount == rows.Length) && (totalAmount == total.TotalAmount) && (totalTransferSum == total.TotalTransferSum) && (totalCommissionSum == total.TotalCommissionSum));
        }

        //internal static void CustomizeGrid(DataGridView dgv)
        //{
        //    GridTools.AdjustColumn(dgv.Columns["colIndex"], "Номер строки", 50, 0);
        //    GridTools.AdjustColumn(dgv.Columns["colDate"], "Дата платежа", 50, 1);
        //    GridTools.AdjustColumn(dgv.Columns["colId"], "Уникальный код операции в ЕПС", 100, 2);
        //    GridTools.AdjustColumn(dgv.Columns["colAccount"], "Лицевой счет", 50, 3, DataGridViewAutoSizeColumnMode.Fill);
        //    GridTools.AdjustColumn(dgv.Columns["colPaymentSum"], "Сумма по услуге", 100, 4);
        //    GridTools.AdjustColumn(dgv.Columns["colAmount"], "Общая сумма платежа", 100, 5);
        //    GridTools.AdjustColumn(dgv.Columns["colTransferSum"], "Общая сумма перевода", 100, 6);
        //    GridTools.AdjustColumn(dgv.Columns["colCommissionSum"], "Сумма комиссию банку от общей суммы", 100, 7);
        //    GridTools.HideColumn(dgv.Columns["colStatus"]);

        //    dgv.CellPainting += dgv_CellPainting;
        //}

        //private static void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        //{
        //    if (e.RowIndex == -1) return;
        //    Color fontColor = GridTools.GetRowColor(
        //        (RowStatus)int.Parse(((DataGridView)sender).Rows[e.RowIndex].Cells["colStatus"].Value.ToString()));
        //    e.CellStyle.ForeColor = fontColor;
        //    e.CellStyle.SelectionForeColor = fontColor;
        //}

        //internal static void FillGrid(DataGridView dgv, Row[] rows)
        //{
        //    if ((rows == null) || (dgv == null)) return;
        //    dgv.DataSource = null;
        //    var dt = PrepareDataTable();
        //    try
        //    {
        //        foreach (var payLine in rows)
        //        {
        //            payLine.AddTo(dt);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception($"Ошибка преобразования данных: {e.Message}");
        //    }
        //    dgv.DataSource = dt;
        //    CustomizeGrid(dgv);
        //}
    }
}
