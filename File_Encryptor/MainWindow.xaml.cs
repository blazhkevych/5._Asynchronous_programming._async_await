using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace File_Encryptor;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly CancellationTokenSource
        cts = new(); // попробовать вынести в зону видимости класса и из события клика "отмены" вызывать cts.Cancel();

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
        //FilePath.Text = "";
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
            Console.WriteLine(ex.Message);
        }
        finally
        {
            cts.Dispose();
        }
    }

    // кнопка "Отмена", tru - отмена, false - продолжить
    //private bool _cancelEncryption = false;
    private async Task XorFileEncryption(string filePath, byte encryptionKey, CancellationToken token)
    {
        await Task.Run(() =>
        {
            // Read all bytes from the specified file into a byte array
            var fileBytes = File.ReadAllBytes(filePath);

            long totalBytesRead = 0;
            var fileSize = fileBytes.Length;

            // Loop through each byte in the byte array and XOR it with the encryption key
            for (var i = 0; i < fileBytes.Length; i++)
            {
                //if (_cancelEncryption)
                // todo: посмотреть к месту ли эта ниже строка тут, пересмотреть видео по примеру отмены и подумать куда вставить "cts.Cancel();"
                token.ThrowIfCancellationRequested(); // TODO ошибка.

                fileBytes[i] = (byte)(fileBytes[i] ^ encryptionKey);
                totalBytesRead++;
                Thread.Sleep(500);

                var percentage = (int)(totalBytesRead * 100 / fileSize);
                UiContext.Send(d => ProgressBar.Value = percentage, null);
            }

            // Overwrite the original file with the encrypted/decrypted bytes
            File.WriteAllBytes(filePath, fileBytes);
            UiContext.Send(d => StartEncryptButton.IsEnabled = true, null);
            UiContext.Send(d => CancelButton.IsEnabled = false, null);
        }, token);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        //_cancelEncryption = true;
        cts.Cancel();
    }
}