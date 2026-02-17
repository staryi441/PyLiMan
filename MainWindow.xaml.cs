using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using PyLiMan.Properties; // обязательно для Settings

namespace PyLiMan
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			RefreshInstalled();

			// Загружаем сохранённую тему
			string savedTheme = Settings.Default.SelectedTheme;
			if (!string.IsNullOrEmpty(savedTheme))
			{
				ApplyTheme(savedTheme);
				ThemeSelector.SelectedIndex = savedTheme == "Тёмная" ? 1 : 0;
			}
			else
			{
				ThemeSelector.SelectedIndex = 0; // по умолчанию светлая
			}
		}

		// Установка библиотеки
		private async void Install_Click(object sender, RoutedEventArgs e)
		{
			string package = InstallBox.Text;
			if (string.IsNullOrWhiteSpace(package)) return;
			await RunPipCommand($"install {package}");
		}

		// Удаление библиотеки
		private async void Uninstall_Click(object sender, RoutedEventArgs e)
		{
			string package = InstallBox.Text;
			if (string.IsNullOrWhiteSpace(package)) return;
			await RunPipCommand($"uninstall -y {package}");
		}

		// Обновление библиотеки
		private async void Update_Click(object sender, RoutedEventArgs e)
		{
			string package = InstallBox.Text;
			if (string.IsNullOrWhiteSpace(package)) return;
			await RunPipCommand($"install --upgrade {package}");
		}

		// Запуск pip команды в фоне с прогрессом
		private async Task RunPipCommand(string args)
		{
			InstallProgress.Value = 0;
			InstallPercent.Text = "0%";

			var timer = new System.Timers.Timer(50);
			timer.Elapsed += (s, ev) =>
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					if (InstallProgress.Value < 95)
					{
						InstallProgress.Value += 1;
						InstallPercent.Text = $"{InstallProgress.Value}%";
					}
				});
			};
			timer.Start();

			await Task.Run(() =>
			{
				var process = new Process();
				process.StartInfo.FileName = "python";
				process.StartInfo.Arguments = $"-m pip {args}";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.Start();
				process.WaitForExit();
			});

			timer.Stop();
			InstallProgress.Value = 100;
			InstallPercent.Text = "100%";

			RefreshInstalled();
		}

		// Обновление списка установленных библиотек
		private async void RefreshInstalled()
		{
			InstalledList.Items.Clear();

			await Task.Run(() =>
			{
				var process = new Process();
				process.StartInfo.FileName = "python";
				process.StartInfo.Arguments = "-m pip list --format=freeze";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.Start();
				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

				Application.Current.Dispatcher.Invoke(() =>
				{
					foreach (var line in lines)
						InstalledList.Items.Add(line);
				});
			});
		}

		// Автозаполнение поля при выборе библиотеки
		private void InstalledList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (InstalledList.SelectedItem != null)
			{
				string fullText = InstalledList.SelectedItem.ToString();
				string name = fullText.Split('=')[0].Trim();
				InstallBox.Text = name;
			}
		}

		// Смена темы
		private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ThemeSelector.SelectedItem == null) return;

			string theme = ((ComboBoxItem)ThemeSelector.SelectedItem).Content.ToString();
			ApplyTheme(theme);

			// Сохраняем выбор пользователя
			Settings.Default.SelectedTheme = theme;
			Settings.Default.Save();
		}

		// Применение темы с SolidColorBrush
		private void ApplyTheme(string theme)
		{
			if (theme == "Тёмная")
			{
				Application.Current.Resources["WindowBackgroundColor"] =
					new System.Windows.Media.SolidColorBrush(
						(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF1E1E1E"));
				Application.Current.Resources["TextColor"] =
					new System.Windows.Media.SolidColorBrush(
						(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFFFF"));
			}
			else
			{
				Application.Current.Resources["WindowBackgroundColor"] =
					new System.Windows.Media.SolidColorBrush(
						(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFFFF"));
				Application.Current.Resources["TextColor"] =
					new System.Windows.Media.SolidColorBrush(
						(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF000000"));
			}
		}
	}
}
