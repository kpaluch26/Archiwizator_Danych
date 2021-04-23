using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserConfiguration user_configuration;

        public MainWindow()
        {
            InitializeComponent();
            SetUp();
        }

        private void SetUp()
        {
            user_configuration = new UserConfiguration();
            user_configuration.state = false;
        }        

        private void grd_MenuToolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btn_MenuClose_Click(object sender, RoutedEventArgs e)
        {
            btn_MenuOpen.Visibility = Visibility.Visible;
            btn_MenuClose.Visibility = Visibility.Collapsed;
        }

        private void btn_MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            btn_MenuOpen.Visibility = Visibility.Collapsed;
            btn_MenuClose.Visibility = Visibility.Visible;
        }

        private void btn_ConfigurationSave_Click(object sender, RoutedEventArgs e)
        {
            string firstname, lastname, group, section, version;

            try
            {
                firstname = txt_ConfigurationName.Text.Trim();
                if (firstname != "")
                {
                    user_configuration.firstname = firstname;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano imienia użytkownika.";
                    throw new Exception();
                }

                lastname = txt_ConfigurationSurname.Text.Trim();
                if (lastname != "")
                {
                    user_configuration.lastname = lastname;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano nazwiska użytkownika.";
                    throw new Exception();
                }

                group = txt_ConfigurationGroup.Text.Trim();
                if (group != "")
                {
                    user_configuration.group = group;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano grupy studenckiej użytkownika.";
                    throw new Exception();
                }

                if (cbx_ConfigurationSection.IsChecked == true)
                {
                    section = txt_ConfigurationSection.Text.Trim();
                    if (section != "")
                    {
                        user_configuration.section = section;
                    }
                    else
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie podano sekcji użytkownika.";
                        throw new Exception();
                    }
                }
                else
                {
                    user_configuration.section = null;
                }

                if (cbx_ConfigurationVersion.IsChecked == true)
                {
                    version = txt_ConfigurationVersion.Text.Trim();
                    if (version != "")
                    {
                        user_configuration.version = version;
                    }
                    else
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie podano wersji pracy użytkownika.";
                        throw new Exception();
                    }
                }
                else
                {
                    user_configuration.version = null;
                }

                user_configuration.state = true;
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
            }
            catch
            {
                user_configuration.state = false;
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationConnect_Click(object sender, RoutedEventArgs e)
        {
            bool result = ConnectionStart.TryConnect(user_configuration, this);

            if (result)
            {
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;

                btn_ConfigurationSave.IsEnabled = false;

                btn_ConfigurationConnect.Visibility = Visibility.Collapsed;
                btn_ConfigurationConnect.IsEnabled = false;

                btn_ConfigurationDisconnect.IsEnabled = true;
                btn_ConfigurationDisconnect.Visibility = Visibility.Visible;
            }
            else
            {
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationSetSavePath_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
            fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
            fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

            if (fbd.ShowDialog() == true) //jeśli wybrano ścieżkę
            {
                user_configuration.folderpath = fbd.SelectedPath;
                tbl_ConfigurationSavePath.Text = fbd.SelectedPath;
                tbl_ConfigurationAllert.Text = "";
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
            }
            else
            {
                if (user_configuration.folderpath != null)
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie wybrano nowego miejsca zapisu. Powrót do poprzedniego zapisu.";
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie wybrano nowego miejsca zapisu.";
                }
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationDisconnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionEnd.TryDisconnect(this);
        }
    }
}
