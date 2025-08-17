using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using WinForms = System.Windows.Forms;

namespace analysCAR
{
    /// <summary>
    /// 価格の推移を表示するメインウィンドウ
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "データフォルダを選択してください"
            };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                LoadDataFromDirectory(dialog.SelectedPath);
            }
        }

        private void Grid_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (paths.Length > 0 && Directory.Exists(paths[0]))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (paths.Length > 0 && Directory.Exists(paths[0]))
                {
                    LoadDataFromDirectory(paths[0]);
                }
            }
        }

        private void LoadDataFromDirectory(string dir)
        {
            try
            {
                var points = new List<DataPoint>();
                foreach (var dateDir in Directory.GetDirectories(dir))
                {
                    var csv = Directory.GetFiles(dateDir, "*.csv").FirstOrDefault();
                    if (csv == null) continue;

                    var prices = new List<double>();
                    foreach (var line in File.ReadLines(csv).Skip(1))
                    {
                        var cols = line.Split(',');
                        if (cols.Length > 3)
                        {
                            var priceStr = cols[3]
                                .Replace("万円", string.Empty)
                                .Replace(",", string.Empty)
                                .Trim();
                            if (double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                            {
                                prices.Add(price);
                            }
                        }
                    }

                    if (prices.Count > 0 &&
                        DateTime.TryParseExact(Path.GetFileName(dateDir), "yyyy年MM月dd日", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        points.Add(new DataPoint(DateTimeAxis.ToDouble(dt), prices.Average()));
                    }
                }

                points = points.OrderBy(p => p.X).ToList();

                var model = new PlotModel { Title = "平均支払総額の推移（万円）" };
                model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "yyyy/MM/dd", Title = "日付" });
                model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "価格（万円）" });
                var series = new LineSeries { MarkerType = MarkerType.Circle };
                series.Points.AddRange(points);
                model.Series.Add(series);

                plotView.Model = model;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"読み込み中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

