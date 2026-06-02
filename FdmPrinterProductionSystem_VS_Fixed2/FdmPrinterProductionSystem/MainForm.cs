using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace FdmPrinterProductionSystem
{
    public class MainForm : Form
    {
        private readonly List<Action> _reloadActions = new List<Action>();
        private readonly Dictionary<string, decimal> _printerPrices = new Dictionary<string, decimal>();
        private TabControl _tabs;
        private Label _statusLabel;

        public MainForm()
        {
            Text = "Производство FDM 3D-принтеров";
            Width = 1220;
            Height = 740;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(850, 560);

            _printerPrices.Add("FDM Home Mini", 34900m);
            _printerPrices.Add("FDM Home Standard", 42900m);
            _printerPrices.Add("FDM Home Pro", 49900m);
            _printerPrices.Add("FDM Home Pro Plus", 59900m);

            try
            {
                Database.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось подготовить JSON-файл с данными.\n\n" +
                    "Программа работает без SQL Server и хранит данные в файле fdm_production.json.\n\n" +
                    "Текст ошибки:\n" + ex.Message,
                    "Ошибка JSON-файла",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            BuildInterface();
        }

        private void BuildInterface()
        {
            Controls.Clear();
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            Controls.Add(layout);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(245, 245, 245);
            layout.Controls.Add(header, 0, 0);

            Label title = new Label();
            title.Text = "Информационная подсистема «Производство FDM 3D-принтеров для домашнего использования»";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(16, 10);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Клиент, менеджер, поставщики, проектирование, бухгалтер, производство, тестирование, упаковка, оплата и транспортная компания";
            subtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(17, 36);
            header.Controls.Add(subtitle);

            _tabs = new TabControl();
            _tabs.Dock = DockStyle.Fill;
            _tabs.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _tabs.Multiline = true;
            _tabs.SizeMode = TabSizeMode.Fixed;
            _tabs.ItemSize = new Size(145, 28);
            _tabs.HotTrack = true;
            layout.Controls.Add(_tabs, 0, 1);

            StatusStrip status = new StatusStrip();
            status.Dock = DockStyle.Fill;
            _statusLabel = new Label();
            _statusLabel.AutoSize = true;
            ToolStripControlHost host = new ToolStripControlHost(_statusLabel);
            status.Items.Add(host);
            layout.Controls.Add(status, 0, 2);

            _tabs.TabPages.Add(CreateDashboardTab());
            _tabs.TabPages.Add(CreateOrdersTab());
            _tabs.TabPages.Add(CreateClientsTab());
            _tabs.TabPages.Add(CreateSuppliersTab());
            _tabs.TabPages.Add(CreateDesignsTab());
            _tabs.TabPages.Add(CreateAccountantTab());
            _tabs.TabPages.Add(CreateProductionTab());
            _tabs.TabPages.Add(CreateTestingTab());
            _tabs.TabPages.Add(CreatePackagePaymentTab());
            _tabs.TabPages.Add(CreateTransportCompanyTab());

            ReloadAll();
        }

        private TabPage CreateDashboardTab()
        {
            TabPage page = new TabPage("Главная");

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(18);
            panel.ColumnCount = 1;
            panel.RowCount = 2;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(panel);

            Label info = new Label();
            info.Dock = DockStyle.Fill;
            info.Font = new Font("Segoe UI", 10F);
            info.Text = "На главной вкладке показана сводка по данным системы. Основная работа выполняется по ролям: клиент создает заказ, менеджер отправляет его в проектирование, затем заказ проходит бухгалтера, производство, тестирование, упаковку и транспортную компанию.";
            panel.Controls.Add(info, 0, 0);

            DataGridView grid = CreateGrid();
            grid.Dock = DockStyle.Fill;
            panel.Controls.Add(grid, 0, 1);

            Action reload = delegate
            {
                try
                {
                    DataTable table = new DataTable();
                    table.Columns.Add("Показатель");
                    table.Columns.Add("Значение");
                    table.Rows.Add("Количество клиентов", Database.Count("Clients"));
                    table.Rows.Add("Количество заказов", Database.Count("Orders"));
                    table.Rows.Add("Поставщиков", Database.Count("Suppliers"));
                    table.Rows.Add("Проектных документов", Database.Count("Designs"));
                    table.Rows.Add("Производственных заданий", Database.Count("ProductionTasks"));
                    table.Rows.Add("Протоколов тестирования", Database.Count("TestProtocols"));
                    table.Rows.Add("Записей упаковки и оплаты", Database.Count("PackagePayments"));
                    table.Rows.Add("Запросов в транспортную компанию", Database.Count("ActiveTransportRequests"));
                    grid.DataSource = table;
                    SetStatus("Данные обновлены. JSON-файл: " + Database.DbFile);
                }
                catch (Exception ex)
                {
                    SetStatus("Ошибка обновления главной панели: " + ex.Message);
                }
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateOrdersTab()
        {
            TabPage page = new TabPage("Клиент");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox clientNameBox = CreateTextBox("");
            TextBox phoneBox = CreateTextBox("");
            TextBox emailBox = CreateTextBox("");
            TextBox addressBox = CreateTextBox("");
            ComboBox modelBox = CreatePrinterModelBox();
            NumericUpDown quantityBox = CreateNumber(1, 1, 1000, 0);
            NumericUpDown unitPriceBox = CreateNumber(34900, 0, 10000000, 2);
            NumericUpDown amountBox = CreateNumber(34900, 0, 1000000000, 2);
            unitPriceBox.ReadOnly = true;
            amountBox.ReadOnly = true;

            EventHandler recalc = delegate { RecalculateOrderAmount(modelBox, quantityBox, unitPriceBox, amountBox); };
            modelBox.SelectedIndexChanged += recalc;
            quantityBox.ValueChanged += recalc;
            RecalculateOrderAmount(modelBox, quantityBox, unitPriceBox, amountBox);

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 82;
            info.Text = "Вкладка имитирует действия клиента. Клиент вводит свои контактные данные, выбирает модель принтера и создает заказ. После создания заказ поступает менеджеру.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Ваше имя", clientNameBox));
            form.Controls.Add(CreateField("Телефон", phoneBox));
            form.Controls.Add(CreateField("Email", emailBox));
            form.Controls.Add(CreateField("Адрес", addressBox));
            form.Controls.Add(CreateField("Выберите модель", modelBox));
            form.Controls.Add(CreateField("Количество", quantityBox));
            form.Controls.Add(CreateField("Цена за единицу", unitPriceBox));
            form.Controls.Add(CreateField("Итоговая цена", amountBox));

            Button add = CreateButton("Создать заказ");
            add.Click += delegate
            {
                try
                {
                    string clientName = clientNameBox.Text.Trim();
                    if (String.IsNullOrWhiteSpace(clientName)) throw new Exception("Введите имя клиента.");
                    Database.AddOrderWithClient(clientName, phoneBox.Text.Trim(), emailBox.Text.Trim(), addressBox.Text.Trim(), modelBox.Text, Convert.ToInt32(quantityBox.Value), unitPriceBox.Value);
                    ClearTextBoxes(form);
                    quantityBox.Value = 1;
                    RecalculateOrderAmount(modelBox, quantityBox, unitPriceBox, amountBox);
                    ReloadAll();
                    MessageBox.Show("Заказ создан и отправлен менеджеру.", "Клиент", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(add);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetOrders();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateClientsTab()
        {
            TabPage page = new TabPage("Менеджер");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            ComboBox statusBox = CreateCombo(new string[] { "Оформлен", "Проектирование", "Проверка склада", "Закупка материалов", "Производство", "Тестирование", "Упаковка", "Оплачен", "Транспортная компания", "Завершен", "Отменен" });

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 100;
            info.Text = "Менеджер работает с заказами, которые создал клиент. Здесь можно изменить статус выбранного заказа или отправить его в проектирование. После отправки строка автоматически появится во вкладке Проектирование.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Новый статус заказа", statusBox));

            Button updateStatus = CreateButton("Изменить статус выбранного заказа");
            updateStatus.Click += delegate
            {
                try
                {
                    Database.UpdateOrderStatus(SelectedId(grid), statusBox.Text);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(updateStatus);

            Button sendToDesign = CreateButton("Отправить выбранный заказ в проектирование");
            sendToDesign.Click += delegate
            {
                try
                {
                    Database.SendOrderToDesign(SelectedId(grid));
                    ReloadAll();
                    MessageBox.Show("Заказ отправлен проектировщику. Во вкладке Проектирование создана строка для заполнения документации.", "Менеджер", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(sendToDesign);

            split.Panel2.Controls.Add(form);

            Action reload = delegate { grid.DataSource = Database.GetOrders(); };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateMaterialsTab()
        {
            TabPage page = new TabPage("Склад");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox nameBox = CreateTextBox("");
            TextBox unitBox = CreateTextBox("шт.");
            NumericUpDown quantityBox = CreateNumber(0, 0, 100000, 2);
            NumericUpDown minBox = CreateNumber(0, 0, 100000, 2);
            ComboBox supplierBox = CreateLookupBox();

            FlowLayoutPanel form = CreateFormPanel();
            form.Controls.Add(CreateField("Материал", nameBox));
            form.Controls.Add(CreateField("Ед. изм.", unitBox));
            form.Controls.Add(CreateField("Остаток", quantityBox));
            form.Controls.Add(CreateField("Мин. остаток", minBox));
            form.Controls.Add(CreateField("Поставщик", supplierBox));

            Button add = CreateButton("Добавить материал");
            add.Click += delegate
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(nameBox.Text)) throw new Exception("Введите название материала.");
                    int? supplierId = NullableLookupId(supplierBox);
                    Database.AddMaterial(nameBox.Text.Trim(), unitBox.Text.Trim(), quantityBox.Value, minBox.Value, supplierId.HasValue ? (object)supplierId.Value : null);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(add);

            Button update = CreateButton("Обновить остаток выбранного материала");
            update.Click += delegate
            {
                try
                {
                    int id = SelectedId(grid);
                    Database.UpdateMaterialQuantity(id, quantityBox.Value);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(update);

            Button delete = CreateButton("Удалить выбранный материал");
            delete.Click += delegate { DeleteSelected(grid, "Materials"); };
            form.Controls.Add(delete);
            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetMaterials();
                supplierBox.DisplayMember = "Name";
                supplierBox.ValueMember = "Id";
                supplierBox.DataSource = Database.GetSupplierLookup();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateSuppliersTab()
        {
            TabPage page = new TabPage("Поставщики");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox nameBox = CreateTextBox("");
            TextBox contactBox = CreateTextBox("");
            TextBox phoneBox = CreateTextBox("");
            TextBox emailBox = CreateTextBox("");

            FlowLayoutPanel form = CreateFormPanel();
            form.Controls.Add(CreateField("Название", nameBox));
            form.Controls.Add(CreateField("Контактное лицо", contactBox));
            form.Controls.Add(CreateField("Телефон", phoneBox));
            form.Controls.Add(CreateField("Email", emailBox));

            Button add = CreateButton("Добавить поставщика");
            add.Click += delegate
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(nameBox.Text)) throw new Exception("Введите название поставщика.");
                    Database.AddSupplier(nameBox.Text.Trim(), contactBox.Text.Trim(), phoneBox.Text.Trim(), emailBox.Text.Trim());
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(add);

            Button delete = CreateButton("Удалить выбранного поставщика");
            delete.Click += delegate { DeleteSelected(grid, "Suppliers"); };
            form.Controls.Add(delete);
            split.Panel2.Controls.Add(form);

            Action reload = delegate { grid.DataSource = Database.GetSuppliers(); };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateDesignsTab()
        {
            TabPage page = new TabPage("Проектирование");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox drawingBox = CreateTextBox("Чертеж корпуса и кинематики");
            TextBox specificationBox = CreateMultilineTextBox("Корпус, направляющие, плата управления, датчики, стол печати");
            TextBox engineerBox = CreateTextBox("");

            grid.SelectionChanged += delegate
            {
                try
                {
                    if (grid.CurrentRow == null) return;
                    if (grid.CurrentRow.Cells["Чертеж"].Value != null) drawingBox.Text = grid.CurrentRow.Cells["Чертеж"].Value.ToString();
                    if (grid.CurrentRow.Cells["Спецификация"].Value != null) specificationBox.Text = grid.CurrentRow.Cells["Спецификация"].Value.ToString();
                    if (grid.CurrentRow.Cells["Проектировщик"].Value != null) engineerBox.Text = grid.CurrentRow.Cells["Проектировщик"].Value.ToString();
                }
                catch { }
            };

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 100;
            info.Text = "Строки проектирования создаются менеджером при отправке заказа. Проектировщик выбирает строку, заполняет данные и отправляет заказ бухгалтеру на проверку материалов.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Название чертежа", drawingBox));
            form.Controls.Add(CreateField("Спецификация", specificationBox));
            form.Controls.Add(CreateField("Проектировщик", engineerBox));

            Button save = CreateButton("Сохранить данные проектирования");
            save.Click += delegate
            {
                try
                {
                    Database.UpdateDesign(SelectedId(grid), drawingBox.Text.Trim(), specificationBox.Text.Trim(), engineerBox.Text.Trim());
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(save);

            Button pass = CreateButton("Отправить заказ бухгалтеру");
            pass.Click += delegate
            {
                try
                {
                    int designId = SelectedId(grid);
                    Database.UpdateDesign(designId, drawingBox.Text.Trim(), specificationBox.Text.Trim(), engineerBox.Text.Trim());
                    Database.PassDesign(designId);
                    ReloadAll();
                    MessageBox.Show("Проектирование завершено. Заказ передан бухгалтеру для проверки материалов.", "Проектирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(pass);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetDesigns();
            };
            _reloadActions.Add(reload);
            return page;
        }


        private TabPage CreateAccountantTab()
        {
            TabPage page = new TabPage("Бухгалтер");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            ComboBox orderBox = CreateLookupBox();

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 112;
            info.Text = "Бухгалтер проверяет склад после проектирования. Если материалов хватает, заказ переводится в производство. Если материалов не хватает, заказ переводится на этап закупки материалов.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Заказ для проверки", orderBox));

            Button refresh = CreateButton("Показать потребность по заказу");
            refresh.Click += delegate
            {
                try
                {
                    int orderId = SelectedLookupId(orderBox, "Выберите заказ.");
                    grid.DataSource = Database.GetStockCheckForOrder(orderId);
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(refresh);

            Button check = CreateButton("Проверить склад");
            check.Click += delegate
            {
                try
                {
                    int orderId = SelectedLookupId(orderBox, "Выберите заказ.");
                    string result = Database.CheckStockAndRouteOrder(orderId);
                    grid.DataSource = Database.GetStockCheckForOrder(orderId);
                    MessageBox.Show(result, "Проверка склада", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(check);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                orderBox.DisplayMember = "Title";
                orderBox.ValueMember = "Id";
                orderBox.DataSource = Database.GetOrderLookup();
                if (orderBox.SelectedValue != null && !(orderBox.SelectedValue is DataRowView))
                {
                    try
                    {
                        grid.DataSource = Database.GetStockCheckForOrder(Convert.ToInt32(orderBox.SelectedValue));
                    }
                    catch
                    {
                        grid.DataSource = null;
                    }
                }
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateProductionTab()
        {
            TabPage page = new TabPage("Производство");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox executorBox = CreateTextBox("");
            ComboBox statusBox = CreateCombo(new string[] { "Запланировано", "В работе", "Завершено" });

            grid.SelectionChanged += delegate
            {
                try
                {
                    if (grid.CurrentRow == null) return;
                    if (grid.CurrentRow.Cells["Исполнитель"].Value != null) executorBox.Text = grid.CurrentRow.Cells["Исполнитель"].Value.ToString();
                    if (grid.CurrentRow.Cells["Статус задания"].Value != null)
                    {
                        string status = grid.CurrentRow.Cells["Статус задания"].Value.ToString();
                        if (statusBox.Items.Contains(status)) statusBox.SelectedItem = status;
                    }
                }
                catch { }
            };

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 100;
            info.Text = "Производственные задания создаются автоматически после проверки склада бухгалтером. Здесь изменяется исполнитель и статус выбранного задания. После завершения всех заданий заказ автоматически появится в Тестировании.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Исполнитель", executorBox));
            form.Controls.Add(CreateField("Статус", statusBox));

            Button updateStatus = CreateButton("Сохранить статус выбранного задания");
            updateStatus.Click += delegate
            {
                try
                {
                    Database.UpdateProductionTask(SelectedId(grid), executorBox.Text.Trim(), statusBox.Text);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(updateStatus);

            Button finish = CreateButton("Этап производства пройден");
            finish.Click += delegate
            {
                try
                {
                    Database.UpdateProductionTask(SelectedId(grid), executorBox.Text.Trim(), "Завершено");
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(finish);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetProductionTasks();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateTestingTab()
        {
            TabPage page = new TabPage("Тестирование");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox testerBox = CreateTextBox("");
            ComboBox resultBox = CreateCombo(new string[] { "Ожидает проверки", "Пройдено", "Требуется доработка", "Не пройдено" });
            TextBox commentBox = CreateMultilineTextBox("Проверка электроники, точности печати и комплектации выполнена.");

            grid.SelectionChanged += delegate
            {
                try
                {
                    if (grid.CurrentRow == null) return;
                    if (grid.CurrentRow.Cells["Тестировщик"].Value != null) testerBox.Text = grid.CurrentRow.Cells["Тестировщик"].Value.ToString();
                    if (grid.CurrentRow.Cells["Результат"].Value != null)
                    {
                        string result = grid.CurrentRow.Cells["Результат"].Value.ToString();
                        if (resultBox.Items.Contains(result)) resultBox.SelectedItem = result;
                    }
                    if (grid.CurrentRow.Cells["Комментарий"].Value != null) commentBox.Text = grid.CurrentRow.Cells["Комментарий"].Value.ToString();
                }
                catch { }
            };

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 100;
            info.Text = "Строка тестирования создается автоматически после завершения производства. Тестировщик выбирает строку, сохраняет результат и при успешной проверке отправляет заказ на упаковку.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Тестировщик", testerBox));
            form.Controls.Add(CreateField("Результат", resultBox));
            form.Controls.Add(CreateField("Комментарий", commentBox));

            Button save = CreateButton("Сохранить результат проверки");
            save.Click += delegate
            {
                try
                {
                    Database.UpdateTestProtocol(SelectedId(grid), testerBox.Text.Trim(), resultBox.Text, commentBox.Text.Trim());
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(save);

            Button pass = CreateButton("Тестирование пройдено");
            pass.Click += delegate
            {
                try
                {
                    Database.UpdateTestProtocol(SelectedId(grid), testerBox.Text.Trim(), "Пройдено", commentBox.Text.Trim());
                    ReloadAll();
                    MessageBox.Show("Тестирование пройдено. Заказ передан на упаковку и оплату.", "Тестирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(pass);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetTestProtocols();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreatePackagePaymentTab()
        {
            TabPage page = new TabPage("Упаковка и оплата");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            TextBox packagerBox = CreateTextBox("");
            TextBox deliveryBox = CreateTextBox("TK-");
            CheckBox paidBox = new CheckBox();
            paidBox.Text = "Заказ оплачен";
            paidBox.Width = 300;

            grid.SelectionChanged += delegate
            {
                try
                {
                    if (grid.CurrentRow == null) return;
                    if (grid.CurrentRow.Cells["Упаковщик"].Value != null) packagerBox.Text = grid.CurrentRow.Cells["Упаковщик"].Value.ToString();
                    if (grid.CurrentRow.Cells["Код доставки"].Value != null) deliveryBox.Text = grid.CurrentRow.Cells["Код доставки"].Value.ToString();
                    if (grid.CurrentRow.Cells["Оплачено"].Value != null) paidBox.Checked = String.Equals(grid.CurrentRow.Cells["Оплачено"].Value.ToString(), "Да", StringComparison.OrdinalIgnoreCase);
                }
                catch { }
            };

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 100;
            info.Text = "Строка упаковки создается автоматически после успешного тестирования. Упаковщик заполняет данные, отмечает оплату и передает готовый принтер транспортной компании.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Упаковщик", packagerBox));
            form.Controls.Add(CreateField("Код заявки в транспортную компанию", deliveryBox));
            form.Controls.Add(CreateField("Оплата", paidBox));

            Button save = CreateButton("Сохранить упаковку и оплату");
            save.Click += delegate
            {
                try
                {
                    Database.UpdatePackagePayment(SelectedId(grid), packagerBox.Text.Trim(), deliveryBox.Text.Trim(), paidBox.Checked);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(save);

            Button markPaid = CreateButton("Отметить выбранную запись как оплаченную");
            markPaid.Click += delegate
            {
                try
                {
                    Database.SetPackagePaymentPaid(SelectedId(grid), true);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(markPaid);

            Button transport = CreateButton("Передать принтер транспортной компании");
            transport.Click += delegate
            {
                try
                {
                    Database.UpdatePackagePayment(SelectedId(grid), packagerBox.Text.Trim(), deliveryBox.Text.Trim(), paidBox.Checked);
                    Database.AddPrinterTransportRequestFromPackagePayment(SelectedId(grid));
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(transport);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetPackagePayments();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private TabPage CreateTransportCompanyTab()
        {
            TabPage page = new TabPage("Транспортная компания");
            SplitContainer split = CreateSplit(page);
            DataGridView grid = CreateGrid();
            split.Panel1.Controls.Add(grid);

            ComboBox requestTypeBox = CreateCombo(new string[] { "Доставка материалов от поставщика", "Доставка принтера клиенту" });
            ComboBox supplierBox = CreateLookupBox();
            ComboBox materialBox = CreateLookupBox();
            ComboBox orderBox = CreateLookupBox();
            TextBox codeBox = CreateTextBox("TK-");
            NumericUpDown quantityBox = CreateNumber(1, 0, 100000, 2);
            ComboBox statusBox = CreateCombo(new string[] { "Заявка передана в транспортную компанию", "В пути", "Доставлено", "Отменено" });

            requestTypeBox.SelectedIndexChanged += delegate
            {
                quantityBox.Enabled = requestTypeBox.Text.IndexOf("материал", StringComparison.OrdinalIgnoreCase) >= 0;
            };
            quantityBox.Enabled = requestTypeBox.Text.IndexOf("материал", StringComparison.OrdinalIgnoreCase) >= 0;

            FlowLayoutPanel form = CreateFormPanel();

            Label info = new Label();
            info.Width = 315;
            info.Height = 86;
            info.Text = "Вкладка отражает работу транспортной компании. Для доставки материалов нужно указать количество: после статуса \"Доставлено\" материал прибавится на склад, а во вкладке Производство появится завершенное задание по получению материалов.";
            form.Controls.Add(info);

            form.Controls.Add(CreateField("Тип запроса", requestTypeBox));
            form.Controls.Add(CreateField("Поставщик материалов", supplierBox));
            form.Controls.Add(CreateField("Материал", materialBox));
            form.Controls.Add(CreateField("Количество материала", quantityBox));
            form.Controls.Add(CreateField("Заказ клиента", orderBox));
            form.Controls.Add(CreateField("Код заявки", codeBox));
            form.Controls.Add(CreateField("Статус", statusBox));

            Button add = CreateButton("Создать запрос в транспортную компанию");
            add.Click += delegate
            {
                try
                {
                    int? orderId = NullableLookupId(orderBox);
                    int? supplierId = NullableLookupId(supplierBox);
                    int? materialId = NullableLookupId(materialBox);
                    Database.AddTransportRequest(requestTypeBox.Text, orderId, supplierId, materialId, quantityBox.Value, codeBox.Text.Trim(), statusBox.Text);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(add);

            Button update = CreateButton("Изменить статус выбранного запроса");
            update.Click += delegate
            {
                try
                {
                    Database.UpdateTransportRequestStatus(SelectedId(grid), statusBox.Text);
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(update);

            Button finish = CreateButton("Закрыть выбранную доставку");
            finish.Click += delegate
            {
                try
                {
                    Database.CompleteTransportRequest(SelectedId(grid));
                    ReloadAll();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            form.Controls.Add(finish);

            Button delete = CreateButton("Удалить выбранный запрос");
            delete.Click += delegate { DeleteSelected(grid, "TransportRequests"); };
            form.Controls.Add(delete);

            split.Panel2.Controls.Add(form);

            Action reload = delegate
            {
                grid.DataSource = Database.GetTransportRequests();

                supplierBox.DisplayMember = "Name";
                supplierBox.ValueMember = "Id";
                supplierBox.DataSource = Database.GetSupplierLookup();

                materialBox.DisplayMember = "Name";
                materialBox.ValueMember = "Id";
                materialBox.DataSource = Database.GetMaterialLookup();

                orderBox.DisplayMember = "Title";
                orderBox.ValueMember = "Id";
                orderBox.DataSource = Database.GetOrderLookup();
            };
            _reloadActions.Add(reload);
            return page;
        }

        private SplitContainer CreateSplit(TabPage page)
        {
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel2;

            // В момент создания вкладки SplitContainer еще не получил реальную ширину.
            // Поэтому нельзя сразу ставить большой Panel2MinSize: на некоторых экранах
            // Windows Forms выбрасывает InvalidOperationException до открытия окна.
            split.Panel1MinSize = 80;
            split.Panel2MinSize = 80;

            split.Panel1.Padding = new Padding(10);
            split.Panel2.Padding = new Padding(8);
            page.Controls.Add(split);

            EventHandler resize = delegate { KeepRightPanelVisible(split); };
            split.SizeChanged += resize;
            page.SizeChanged += resize;
            split.HandleCreated += delegate { KeepRightPanelVisible(split); };
            return split;
        }

        private void KeepRightPanelVisible(SplitContainer split)
        {
            if (split == null || split.Width <= 0) return;

            int splitterWidth = split.SplitterWidth;
            int totalWidth = split.Width - splitterWidth;
            if (totalWidth <= 0) return;

            // Правая панель нужна для формы ввода. Левая панель с БД уменьшается первой.
            // При узком окне правая панель тоже аккуратно сжимается, но не ломает запуск.
            int desiredRightWidth = 330;
            int minRightWidth = 220;
            int minLeftWidth = 120;

            int rightWidth = desiredRightWidth;
            if (totalWidth - rightWidth < minLeftWidth)
            {
                rightWidth = totalWidth - minLeftWidth;
            }
            if (rightWidth < minRightWidth)
            {
                rightWidth = Math.Max(80, totalWidth / 2);
            }

            int panel1Min = Math.Min(minLeftWidth, Math.Max(0, totalWidth - 80));
            int panel2Min = Math.Min(80, Math.Max(0, totalWidth - panel1Min));

            try
            {
                split.Panel1MinSize = panel1Min;
                split.Panel2MinSize = panel2Min;

                int desiredDistance = totalWidth - rightWidth;
                int minDistance = split.Panel1MinSize;
                int maxDistance = split.Width - split.Panel2MinSize - splitterWidth;

                if (desiredDistance < minDistance) desiredDistance = minDistance;
                if (desiredDistance > maxDistance) desiredDistance = maxDistance;

                if (desiredDistance > 0 && desiredDistance != split.SplitterDistance)
                {
                    split.SplitterDistance = desiredDistance;
                }
            }
            catch
            {
                // Защита нужна только для промежуточных состояний отрисовки окна.
            }
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            return grid;
        }

        private FlowLayoutPanel CreateFormPanel()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.Padding = new Padding(0, 4, 0, 0);
            panel.Resize += delegate { ResizeFormChildren(panel); };
            return panel;
        }

        private void ResizeFormChildren(FlowLayoutPanel panel)
        {
            int width = panel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8;
            if (width < 240) width = 240;

            foreach (Control child in panel.Controls)
            {
                child.Width = width;
                if (child is Panel)
                {
                    foreach (Control inner in child.Controls)
                    {
                        if (!(inner is Label)) inner.Width = width - 8;
                    }
                }
            }
        }

        private Panel CreateField(string labelText, Control control)
        {
            Panel panel = new Panel();
            panel.Width = 310;
            panel.Height = control is TextBox && ((TextBox)control).Multiline ? 112 : 62;

            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Location = new Point(0, 0);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            panel.Controls.Add(label);

            control.Location = new Point(0, 24);
            control.Width = 300;
            panel.Controls.Add(control);
            return panel;
        }

        private TextBox CreateTextBox(string text)
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.Width = 300;
            return box;
        }

        private TextBox CreateMultilineTextBox(string text)
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.Width = 300;
            box.Height = 72;
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Vertical;
            return box;
        }

        private ComboBox CreateCombo(string[] values)
        {
            ComboBox box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = 300;
            box.Items.AddRange(values);
            if (box.Items.Count > 0) box.SelectedIndex = 0;
            return box;
        }

        private ComboBox CreatePrinterModelBox()
        {
            ComboBox box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = 300;
            foreach (string model in _printerPrices.Keys)
            {
                box.Items.Add(model);
            }
            if (box.Items.Count > 0) box.SelectedIndex = 0;
            return box;
        }

        private ComboBox CreateLookupBox()
        {
            ComboBox box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = 300;
            return box;
        }

        private ComboBox CreateEditableLookupBox()
        {
            ComboBox box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDown;
            box.Width = 300;
            return box;
        }

        private NumericUpDown CreateNumber(decimal value, decimal min, decimal max, int decimals)
        {
            NumericUpDown number = new NumericUpDown();
            number.Minimum = min;
            number.Maximum = max;
            number.DecimalPlaces = decimals;
            number.Value = value;
            number.Width = 300;
            return number;
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 300;
            button.Height = 34;
            button.Margin = new Padding(0, 6, 0, 0);
            return button;
        }

        private void RecalculateOrderAmount(ComboBox modelBox, NumericUpDown quantityBox, NumericUpDown unitPriceBox, NumericUpDown amountBox)
        {
            decimal price = 0m;
            if (modelBox.SelectedItem != null && _printerPrices.ContainsKey(modelBox.SelectedItem.ToString()))
            {
                price = _printerPrices[modelBox.SelectedItem.ToString()];
            }
            unitPriceBox.Value = price;
            amountBox.Value = price * quantityBox.Value;
        }

        private int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                throw new Exception("Выберите строку в таблице.");
            }
            object value = grid.CurrentRow.Cells["Id"].Value;
            return Convert.ToInt32(value);
        }

        private int SelectedLookupId(ComboBox box, string errorText)
        {
            int? value = NullableLookupId(box);
            if (!value.HasValue) throw new Exception(errorText);
            return value.Value;
        }

        private int? NullableLookupId(ComboBox box)
        {
            if (box.SelectedValue == null || box.SelectedValue == DBNull.Value || box.SelectedValue is DataRowView)
            {
                return null;
            }
            return Convert.ToInt32(box.SelectedValue);
        }

        private void DeleteSelected(DataGridView grid, string tableName)
        {
            try
            {
                int id = SelectedId(grid);
                DialogResult result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
                Database.DeleteById(tableName, id);
                ReloadAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить запись. Текст ошибки:\n\n" + ex.Message,
                    "Удаление невозможно",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ClearTextBoxes(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBox)
                {
                    ((TextBox)control).Text = String.Empty;
                }
                if (control.HasChildren)
                {
                    ClearTextBoxes(control);
                }
            }
        }

        private void ReloadAll()
        {
            for (int i = 0; i < _reloadActions.Count; i++)
            {
                try
                {
                    _reloadActions[i]();
                }
                catch (Exception ex)
                {
                    SetStatus("Ошибка обновления данных: " + ex.Message);
                }
            }
        }

        private void ShowError(Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = text;
            }
        }
    }
}
