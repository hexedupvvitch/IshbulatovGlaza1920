using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace IshbulatovGlaza1920
{
    public partial class agentPage : Page
    {

        int CountRecords, CountPage, CurrentPage = 0;
        const int RecordsPage = 10; //количество записей на странице всегда 10
        List<Agent> CurrentPageList = new List<Agent>();
        List<Agent> TableList;

        public agentPage()
        {
            InitializeComponent();
            LoadAgents();
        }

        private void LoadAgents()
        {
            // Загрузка типов из БД
            var agentTypes = IshbulatovGlazaEntities.GetContext().AgentType.ToList();

            // Добавляем "Все типы" первым
            agentTypes.Insert(0, new AgentType { ID = 0, Title = "Все типы" });

            ComboType.ItemsSource = agentTypes;
            ComboType.DisplayMemberPath = "Title";
            ComboType.SelectedValuePath = "ID";
            ComboType.SelectedIndex = 0;
            ComboSort.SelectedIndex = 0;

            Upd();
        }

        private void Upd()
        {
            var currentAgent = IshbulatovGlazaEntities.GetContext().Agent.ToList();

            // Фильтрация по типам из БД
            if (ComboType.SelectedIndex > 0 && ComboType.SelectedItem is AgentType selectedType)
            {
                currentAgent = currentAgent.Where(p => p.AgentTypeID == selectedType.ID).ToList();
            }

            // Поиск
            string NormalniyPhone(string Phone)
            {
                return Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
            }

            currentAgent = currentAgent.Where(p => p.Title.ToLower().Contains(TBoxSearch.Text.ToLower()) ||
                NormalniyPhone(p.Phone).Contains(NormalniyPhone(TBoxSearch.Text)) ||
                p.Email.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();

            // Сортировка
            if (ComboSort.SelectedIndex == 1)
                currentAgent = currentAgent.OrderBy(p => p.Title).ToList();
            if (ComboSort.SelectedIndex == 2)
                currentAgent = currentAgent.OrderByDescending(p => p.Title).ToList();
            if (ComboSort.SelectedIndex == 3)
                currentAgent = currentAgent.OrderBy(p => p.Discount).ToList();
            if (ComboSort.SelectedIndex == 4)
                currentAgent = currentAgent.OrderByDescending(p => p.Discount).ToList();
            if (ComboSort.SelectedIndex == 5)
                currentAgent = currentAgent.OrderBy(p => p.Priority).ToList();
            if (ComboSort.SelectedIndex == 6)
                currentAgent = currentAgent.OrderByDescending(p => p.Priority).ToList();

            TableList = currentAgent;
            ChangePage(0, 0);
        }

        public void ChangePage(int direction, int? selectedPage)
        {
            CurrentPageList.Clear();
            CountRecords = TableList.Count; //записываем сколько всего записей в таблице
            CountPage = (CountRecords + RecordsPage - 1) / RecordsPage; // вычисляем сколько всего страниц получится

            if (CountPage == 0) CountPage = 1; //если страниц нет, делаем одну пустую

            if (selectedPage.HasValue && selectedPage >= 0 && selectedPage < CountPage)
                CurrentPage = selectedPage.Value; //переходим на выбранную страницу
            else if (direction == 1 && CurrentPage > 0)
                CurrentPage--; //влево
            else if (direction == 2 && CurrentPage < CountPage - 1)
                CurrentPage++; //вправо
            else
                return;

            int start = CurrentPage * RecordsPage; //индекс первого агента на странице
            int end = Math.Min(start + RecordsPage, CountRecords); //чтобы не выйти за границы
            for (int i = start; i < end; i++)
                CurrentPageList.Add(TableList[i]);

            PageListBox.Items.Clear();
            for (int i = 1; i <= CountPage; i++)
                PageListBox.Items.Add(i);
            PageListBox.SelectedIndex = CurrentPage;
            ListViewAgent.ItemsSource = CurrentPageList;
            ListViewAgent.Items.Refresh();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Upd();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Upd();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Upd();
        }

        private void ListViewAgent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedCount = ListViewAgent.SelectedItems.Count;
            ChangePriorityBtn.Visibility = selectedCount >= 1 ? Visibility.Visible : Visibility.Collapsed;

            // Обновляем текст кнопки
            if (selectedCount >= 1)
            {
                int maxPriority = ListViewAgent.SelectedItems.Cast<Agent>().Max(a => a.Priority);
                ChangePriorityBtn.Content = $"Изменить приоритет на {maxPriority}";
            }
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(1, null);
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(2, null);
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                ChangePage(0, Convert.ToInt32(PageListBox.SelectedItem.ToString()) - 1);
            }
        }

        private void AddAgent_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage((sender as Button).DataContext as Agent));
        }

        private async void ChangePriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedAgents = ListViewAgent.SelectedItems.Cast<Agent>().ToList();

            if (selectedAgents.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного агента!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maxPriority = 0;
            foreach (var agent in selectedAgents)
            {
                if (agent.Priority > maxPriority)
                    maxPriority = agent.Priority;
            }

            var inputDialog = new PrioritetWindow1(maxPriority);

            if (inputDialog.ShowDialog() == true)
            {
                int newPriority = inputDialog.NewPriority;

                if (newPriority < 0)
                {
                    MessageBox.Show("Приоритет не может быть отрицательным!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    var context = IshbulatovGlazaEntities.GetContext();

                    foreach (var agent in selectedAgents)
                    {
                        var agentToUpdate = context.Agent.FirstOrDefault(a => a.ID == agent.ID);
                        if (agentToUpdate != null)
                        {
                            agentToUpdate.Priority = newPriority;

                            var priorityHistory = new AgentPriorityHistory
                            {
                                AgentID = agentToUpdate.ID,
                                PriorityValue = newPriority,
                                ChangeDate = DateTime.Now
                            };

                            context.AgentPriorityHistory.Add(priorityHistory);
                        }
                    }

                    await context.SaveChangesAsync();
                    LoadAgents();
                    MessageBox.Show($"Приоритет {selectedAgents.Count} агентов успешно изменен на {newPriority}!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}