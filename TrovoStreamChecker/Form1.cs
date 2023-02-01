using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrovoStreamChecker.Trovo;

namespace TrovoStreamChecker
{
    public partial class Form1 : Form
    {
        readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private readonly string _defStr = "обновляю";
        private readonly string ChannelFileName = "channels.txt";
        private readonly string RaidFileName = "raided.txt";

        private string[] _raidedStreamers;

        public Form1()
        {
            InitializeComponent();
        }

        void UpdateChannels(DataGridView source)
        {
            var table = (DataTable)source.DataSource;
            if (table == null)
                return;

            Parallel.ForEach(table.Rows.Cast<DataRow>(), row =>
            {
                int i = 0;
                while (i <= 10)
                {
                    var streamer = row[0].ToString();
                    try
                    {
                        var chInfo = TrovoChannelChecker.GetInfo(streamer);
                        row[1] = chInfo.category_name;
                        row[2] = chInfo.current_viewers;
                        row[3] = chInfo.is_live ? "✔" : "x";

                        source.Invoke(new Action(() => source.Update()));
                        return;
                    }
                    catch (Exception ex)
                    {
                        row[1] = _defStr;
                        row[2] = _defStr;
                        row[3] = _defStr;
                        i++;
                    }
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridOnline.AutoGenerateColumns = true;
            dataGridOnline.DataError += dataGridView_DataError;

            if (!File.Exists(ChannelFileName))
                File.Create(ChannelFileName);

            _raidedStreamers = File.Exists(RaidFileName)
                ? File.ReadAllLines(RaidFileName)
                : new string[0];

            var channels = File
                .ReadAllLines(ChannelFileName)
                .Distinct()
                .OrderBy(item => item)
                .ToList();

            var table = new DataTable();
            table.Columns.Add("Канал");
            table.Columns.Add("Категория");
            table.Columns.Add("Зрителей");
            table.Columns.Add("Онлайн?");
            if (_raidedStreamers.Length > 0)
                table.Columns.Add("Рейдил");

            dataGridOnline.DataSource = table;

            channels.ForEach(item =>
            {
                var row = new List<object>();
                row.AddRange(new[] { item, _defStr, _defStr, _defStr });
                if (_raidedStreamers.Length > 0)
                    row.Add(string.Join("", _raidedStreamers.Where(r => r.Equals(item, StringComparison.InvariantCultureIgnoreCase)).Select(i => "✔").ToArray()));

                table.Rows.Add(row.ToArray());
            });

            dataGridOnline.Sort(dataGridOnline.Columns[3], System.ComponentModel.ListSortDirection.Ascending);

            new Task(() =>
            {
                while (true)
                {
                    UpdateChannels(dataGridOnline);
                    Thread.Sleep(60 * 1000);
                }
            }, cancelTokenSource.Token).Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            cancelTokenSource.Cancel();
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //DataGridView dataGridView = (DataGridView)sender;
            //dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = DateTime.MinValue;
            //MessageBox.Show("Invalid Date Format", "System Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
