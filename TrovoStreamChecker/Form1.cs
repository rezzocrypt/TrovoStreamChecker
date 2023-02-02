using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            FormClosed += (s, e) => { cancelTokenSource.Cancel(); };
        }

        void UpdateChannels(BindingSource source)
        {
            var table = (DataTable)source.DataSource;
            if (table == null)
                return;

            var rows = table.Rows.Cast<DataRow>().ToArray();

            var result = Parallel.ForEach(rows, row =>
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
                        return;
                    }
                    catch (Exception ex)
                    {
                        row[1] = _defStr;
                        row[2] = 0;
                        row[3] = _defStr;
                        i++;
                    }
                }
            });

            while (!result.IsCompleted) ;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridOnline.DataError += (s, eArgs) => { };

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
            table.Columns.Add(new DataColumn("Зрителей", typeof(long)));
            table.Columns.Add("Онлайн?");
            table.Columns.Add("Рейдил");

            channels.ForEach(item =>
            {
                table.Rows.Add(new object[] { item, _defStr, 0, _defStr,
                    string.Join("", _raidedStreamers.Where(r => r.Equals(item, StringComparison.InvariantCultureIgnoreCase)).Select(i => "✔").ToArray())
                });
            });

            var bindingSource = new BindingSource();
            bindingSource.DataSource = table;
            dataGridOnline.DataSource = bindingSource;
            dataGridOnline.Columns[4].Visible = _raidedStreamers.Length > 0;
            dataGridOnline.Sort(dataGridOnline.Columns[3], System.ComponentModel.ListSortDirection.Ascending);

            dataGridOnline.Refresh();

            new Task(() =>
            {
                while (true)
                {
                    UpdateChannels(bindingSource);
                    Thread.Sleep(60 * 1000);
                }
            }, cancelTokenSource.Token).Start();
        }
    }
}