using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IshbulatovGlaza1920
{
    public partial class PrioritetWindow1 : Window
    {
        public int NewPriority { get; private set; }

        public PrioritetWindow1(int defaultPriority)
        {
            InitializeComponent();
            PriorityTextBox.Text = defaultPriority.ToString();
            PriorityTextBox.SelectAll();
            PriorityTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PriorityTextBox.Text, out int result))
            {
                NewPriority = result;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Введите корректное число!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}