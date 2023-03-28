using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace File_Encryptor;

public partial class MainWindow : Window
{
    private CancellationTokenSource
        cts = new();

    public SynchronizationContext UiContext;

    public MainWindow()
    {
        InitializeComponent();
        // позиция окна по центру
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        // блокировка кнопки "Пуск" при запуске программы
        StartEncryptButton.IsEnabled = false;
        // блокировка кнопки "Отмена" при запуске программы
        CancelButton.IsEnabled = false;
        // Получим контекст синхронизации для текущего потока.
        UiContext = SynchronizationContext.Current;
    }


    // Кнопка "Файл"
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Открывает диалоговое окно для открытия только текстовых файлов.
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Text files (*.txt)|*.txt";
        if (openFileDialog.ShowDialog() == true)
            // Записывает путь к файлу в текстовое поле.
            FilePath.Text = openFileDialog.FileName;
        CheckBeforeStart();
    }

    // Метод контролирует ввод в поле пароля.
    private void PasswordBoxChecker()
    {
        if (PasswordBox.Password != "")
        {
            var pass = Convert.ToInt32(PasswordBox.Password);
            if (PasswordBox.Password.Length > 3)
                PasswordBox.Password = string.Empty;
            else if ((pass < 0) | (pass > 255)) PasswordBox.Password = string.Empty;
        }
    }

    // изменения в поле для пароля
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordBoxChecker();
        CheckBeforeStart();
    }

    // метод проверки на заполненные поля для начала шиврования
    private void CheckBeforeStart()
    {
        if (FilePath.Text.Length == 0 || PasswordBox.Password.Length == 0)
            StartEncryptButton.IsEnabled = false;
        else
            StartEncryptButton.IsEnabled = true;

        if (!File.Exists(FilePath.Text)) StartEncryptButton.IsEnabled = false;
    }

    // изменение поля пути к файлу
    private void FilePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckBeforeStart();
    }

    // кнопка "Пуск"
    private async void StartEncryptButton_Click(object sender, RoutedEventArgs e)
    {
        StartEncryptButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        var filePath = FilePath.Text;
        var encryptionKey = Convert.ToByte(PasswordBox.Password);

        //_cancelEncryption = false;
        try
        {
            await XorFileEncryption(filePath, encryptionKey, cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            cts.Dispose();
            cts = new CancellationTokenSource();
        }
    }

    // кнопка "Отмена", tru - отмена, false - продолжить
    private async Task XorFileEncryption(string filePath, byte encryptionKey, CancellationToken token)
    {
        await Task.Run(() =>
        {
            try
            {
                // Read all bytes from the specified file into a byte array
                var fileBytes = File.ReadAllBytes(filePath);

                long totalBytesRead = 0;
                var fileSize = fileBytes.Length;

                // Loop through each byte in the byte array and XOR it with the encryption key
                for (var i = 0; i < fileBytes.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        MessageBox.Show("Получен запрос на отмену задачи!");
                        break;
                    }

                    fileBytes[i] = (byte)(fileBytes[i] ^ encryptionKey);
                    totalBytesRead++;
                    Thread.Sleep(500);

                    var percentage = (int)(totalBytesRead * 100 / fileSize);
                    UiContext.Send(d => ProgressBar.Value = percentage, null);
                }

                if (token.IsCancellationRequested)
                {
                    UiContext.Send(d => ProgressBar.Value = 0, null);
                    UiContext.Send(d => StartEncryptButton.IsEnabled = true, null);
                    UiContext.Send(d => CancelButton.IsEnabled = false, null);
                    return;
                }

                // Overwrite the original file with the encrypted/decrypted bytes
                File.WriteAllBytes(filePath, fileBytes);
                UiContext.Send(d => StartEncryptButton.IsEnabled = true, null);
                UiContext.Send(d => CancelButton.IsEnabled = false, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }, token);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        cts.Cancel();
    }
}