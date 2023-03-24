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
    public MainWindow()
    {
        InitializeComponent();
        // позиция окна по центру
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        // блокировка кнопки "Пуск" при запуске программы
        StartEncryptButton.IsEnabled = false;
        // блокировка кнопки "Отмена" при запуске программы
        CancelButton.IsEnabled = false;
    }

    public SynchronizationContext uiContext;

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
            int pass = Convert.ToInt32(PasswordBox.Password);
            if (PasswordBox.Password.Length > 3)
            {
                PasswordBox.Password = String.Empty;
            }
            else if (pass < 0 | pass > 255)
            {
                PasswordBox.Password = String.Empty;
            }
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

        if (!File.Exists(FilePath.Text))
        {
            StartEncryptButton.IsEnabled = false;
            //FilePath.Text = "";
        }

    }

    // изменение поля пути к файлу
    private void FilePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        CheckBeforeStart();
    }

    // 
    private async void StartEncryptButton_Click(object sender, RoutedEventArgs e)
    {
        // включить кнопку отмены.
        StartEncryptButton.IsEnabled = false;
        string filePath = FilePath.Text;
        byte encryptionKey = Convert.ToByte(PasswordBox.Password);

        CancellationTokenSource cts = new CancellationTokenSource();

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

    private Task XorFileEncryption(string filePath, byte encryptionKey, CancellationToken token)
    {
        return Task.Run(() =>
        {
            // Read all bytes from the specified file into a byte array
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Loop through each byte in the byte array and XOR it with the encryption key
            for (int i = 0; i < fileBytes.Length; i++)
            {
                fileBytes[i] = (byte)(fileBytes[i] ^ encryptionKey);
                token.ThrowIfCancellationRequested();
            }

            // Overwrite the original file with the encrypted/decrypted bytes
            File.WriteAllBytes(filePath, fileBytes);
        }, token);
    }


}