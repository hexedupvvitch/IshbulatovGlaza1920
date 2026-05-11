using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace IshbulatovGlaza1920
{
    public partial class AddEditPage : Page
    {
        public class SaleHistoryItem
        {
            public Product Product { get; set; }
            public int ProductCount { get; set; }
            public DateTime SaleDate { get; set; }
            public string ProductName => Product?.Title ?? "Неизвестный продукт";
        }

        private Agent currentAgent = new Agent();
        private List<SaleHistoryItem> salesHistory = new List<SaleHistoryItem>();
        private List<Product> products = new List<Product>();

        public AddEditPage(Agent selectedAgent)
        {
            InitializeComponent();

            if (selectedAgent != null)
                currentAgent = selectedAgent;

            DataContext = currentAgent;

            // Загрузка типов агентов
            var agentTypes = IshbulatovGlazaEntities.GetContext().AgentType.ToList();
            ComboType.ItemsSource = agentTypes;
            ComboType.DisplayMemberPath = "Title";
            ComboType.SelectedValuePath = "ID";

            if (currentAgent.AgentTypeID != 0)
                ComboType.SelectedValue = currentAgent.AgentTypeID;

            // Кнопка удалить видна только при редактировании
            DeleteAgent.Visibility = (selectedAgent != null && selectedAgent.ID != 0) ? Visibility.Visible : Visibility.Collapsed;

            // Загрузка продуктов
            LoadProducts();

            // Загрузка истории продаж
            if (currentAgent.ID != 0)
                LoadSalesHistory();

            // Дата по умолчанию
            SaleDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadProducts()
        {
            try
            {
                products = IshbulatovGlazaEntities.GetContext().Product.ToList();
                ProductComboBox.ItemsSource = products;
                ProductComboBox.DisplayMemberPath = "Title";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ex.Message);
            }
        }

        private void LoadSalesHistory()
        {
            try
            {
                var context = IshbulatovGlazaEntities.GetContext();
                var sales = context.ProductSale
                    .Where(ps => ps.AgentID == currentAgent.ID)
                    .ToList();

                salesHistory.Clear();
                foreach (var sale in sales)
                {
                    salesHistory.Add(new SaleHistoryItem
                    {
                        Product = sale.Product,
                        ProductCount = sale.ProductCount,
                        SaleDate = sale.SaleDate
                    });
                }

                SalesListView.ItemsSource = null;
                SalesListView.ItemsSource = salesHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки истории продаж: " + ex.Message);
            }
        }

        private void ChangePictureBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            myOpenFileDialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*";

            if (myOpenFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = myOpenFileDialog.FileName;

                    string imgsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", "agents");
                    Directory.CreateDirectory(imgsFolder);

                    string fileName = Path.GetFileName(sourceFile);
                    string destPath = Path.Combine(imgsFolder, fileName);

                    int count = 1;
                    while (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(imgsFolder, $"{nameWithoutExt}_{count}{ext}");
                        count++;
                    }

                    File.Copy(sourceFile, destPath);
                    currentAgent.Logo = $@"\agents\{Path.GetFileName(destPath)}";
                    LogoImage.Source = new BitmapImage(new Uri(destPath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при копировании файла: " + ex.Message);
                }
            }
        }

        private void DeleteAgent_Click(object sender, RoutedEventArgs e)
        {
            var context = IshbulatovGlazaEntities.GetContext();
            var agent = context.Agent.FirstOrDefault(a => a.ID == currentAgent.ID);

            if (agent == null)
            {
                MessageBox.Show("Агент не найден в базе данных.");
                return;
            }

            // Проверка: есть ли продажи
            bool hasSales = context.ProductSale.Any(ps => ps.AgentID == currentAgent.ID);
            if (hasSales)
            {
                MessageBox.Show("Нельзя удалить агента: есть информация о реализации продукции!");
                return;
            }

            if (MessageBox.Show("Вы хотите удалить агента?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                // Удаляем историю приоритета
                var priorityHistory = context.AgentPriorityHistory.Where(ph => ph.AgentID == currentAgent.ID).ToList();
                foreach (var item in priorityHistory)
                    context.AgentPriorityHistory.Remove(item);

                // Удаляем точки продаж
                var shops = context.Shop.Where(s => s.AgentID == currentAgent.ID).ToList();
                foreach (var shop in shops)
                    context.Shop.Remove(shop);

                // Удаляем агента
                context.Agent.Remove(agent);
                context.SaveChanges();

                MessageBox.Show("Агент удалён.");
                Manager.MainFrame.Navigate(new agentPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message);
            }
        }

        private void SaveAgent_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(currentAgent.Title))
                errors.AppendLine("Укажите наименование агента");
            if (string.IsNullOrWhiteSpace(currentAgent.Address))
                errors.AppendLine("Укажите адрес агента");
            if (string.IsNullOrWhiteSpace(currentAgent.DirectorName))
                errors.AppendLine("Укажите ФИО директора");
            if (ComboType.SelectedItem == null || (ComboType.SelectedItem is AgentType type && type.ID == 0))
                errors.AppendLine("Укажите тип агента");
            else
                currentAgent.AgentTypeID = ((AgentType)ComboType.SelectedItem).ID;

            // Приоритет — целое неотрицательное
            if (currentAgent.Priority < 0)
                errors.AppendLine("Приоритет не может быть отрицательным");
            if (currentAgent.Priority == 0)
                errors.AppendLine("Укажите приоритет агента (положительное число)");

            // ИНН — 12 цифр
            if (string.IsNullOrWhiteSpace(currentAgent.INN))
                errors.AppendLine("Укажите ИНН агента");
            else if (currentAgent.INN.Length != 12 || !currentAgent.INN.All(char.IsDigit))
                errors.AppendLine("ИНН должен содержать ровно 12 цифр");

            // КПП — 9 цифр
            if (string.IsNullOrWhiteSpace(currentAgent.KPP))
                errors.AppendLine("Укажите КПП агента");
            else if (currentAgent.KPP.Length != 9 || !currentAgent.KPP.All(char.IsDigit))
                errors.AppendLine("КПП должен содержать ровно 9 цифр");

            // Телефон
            if (string.IsNullOrWhiteSpace(currentAgent.Phone))
                errors.AppendLine("Укажите телефон агента");
            else
            {
                string ph = new string(currentAgent.Phone.Where(char.IsDigit).ToArray());
                if (ph.Length < 10 || ph.Length > 12)
                    errors.AppendLine("Телефон должен содержать от 10 до 12 цифр");
            }

            // Email
            if (string.IsNullOrWhiteSpace(currentAgent.Email))
                errors.AppendLine("Укажите почту агента");
            else if (!currentAgent.Email.Contains("@") || !currentAgent.Email.Contains("."))
                errors.AppendLine("Укажите корректный email");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var context = IshbulatovGlazaEntities.GetContext();

            try
            {
                if (currentAgent.ID == 0)
                    context.Agent.Add(currentAgent);

                context.SaveChanges();

                // Сохраняем историю продаж
                SaveSalesHistory(context);

                MessageBox.Show("Информация сохранена");
                Manager.MainFrame.Navigate(new agentPage());
            }
            catch (Exception ex)
            {
                string fullError = ex.Message;
                Exception inner = ex.InnerException;
                while (inner != null)
                {
                    fullError += "\n" + inner.Message;

                    // Если это ошибка валидации EF
                    if (inner is System.Data.Entity.Validation.DbEntityValidationException validationEx)
                    {
                        foreach (var entityError in validationEx.EntityValidationErrors)
                        {
                            fullError += $"\nСущность: {entityError.Entry.Entity.GetType().Name}";
                            foreach (var error in entityError.ValidationErrors)
                            {
                                fullError += $"\n  Поле: {error.PropertyName}, Ошибка: {error.ErrorMessage}";
                            }
                        }
                    }
                    inner = inner.InnerException;
                }
                MessageBox.Show(fullError, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSalesHistory(IshbulatovGlazaEntities context)
        {
            try
            {
                // Удаляем старые продажи
                var existingSales = context.ProductSale.Where(ps => ps.AgentID == currentAgent.ID).ToList();
                foreach (var sale in existingSales)
                    context.ProductSale.Remove(sale);
                context.SaveChanges();

                // Добавляем новые
                int maxId = context.ProductSale.Any() ? context.ProductSale.Max(ps => ps.ID) : 0;

                foreach (var saleItem in salesHistory)
                {
                    maxId++;
                    var newSale = new ProductSale
                    {
                        ID = maxId,
                        ProductID = saleItem.Product.ID,
                        AgentID = currentAgent.ID,
                        SaleDate = saleItem.SaleDate,
                        ProductCount = saleItem.ProductCount
                    };
                    context.ProductSale.Add(newSale);
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при сохранении истории продаж: " + ex.Message, ex);
            }
        }

        private void AddSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CountTextBox.Text) || !int.TryParse(CountTextBox.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SaleDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату продажи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProduct = (Product)ProductComboBox.SelectedItem;
            DateTime saleDate = SaleDatePicker.SelectedDate.Value;

            salesHistory.Add(new SaleHistoryItem
            {
                Product = selectedProduct,
                ProductCount = count,
                SaleDate = saleDate
            });

            RefreshListView();
            ClearSaleForm();
        }

        private void DeleteSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var saleToRemove = button?.Tag as SaleHistoryItem;

            if (saleToRemove != null)
            {
                if (MessageBox.Show($"Удалить продажу \"{saleToRemove.ProductName}\" от {saleToRemove.SaleDate:dd.MM.yyyy}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    salesHistory.Remove(saleToRemove);
                    RefreshListView();
                }
            }
        }

        private void RefreshListView()
        {
            SalesListView.ItemsSource = null;
            SalesListView.ItemsSource = salesHistory;
        }

        private void ClearSaleForm()
        {
            ProductComboBox.SelectedItem = null;
            CountTextBox.Text = "";
            SaleDatePicker.SelectedDate = DateTime.Today;
        }

        private void ProductComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            string searchText = comboBox.Text;

            if (string.IsNullOrWhiteSpace(searchText))
                comboBox.ItemsSource = products;
            else
                comboBox.ItemsSource = products.Where(p => p.Title.ToLower().Contains(searchText.ToLower())).ToList();

            comboBox.IsDropDownOpen = true;
        }
    }
}