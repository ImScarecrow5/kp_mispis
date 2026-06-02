using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FdmPrinterProductionSystem
{
    public static class Database
    {
        private static string _dataDirectory;
        private static string _dbFile;
        private static JsonData _data;

        public static string DataDirectory { get { return _dataDirectory; } }
        public static string DbFile { get { return _dbFile; } }
        public static string ConnectionString { get { return "JSON: " + _dbFile; } }

        public static void Initialize()
        {
            _dataDirectory = GetProjectRootDirectory();
            Directory.CreateDirectory(_dataDirectory);
            _dbFile = Path.Combine(_dataDirectory, "fdm_production.json");


            if (File.Exists(_dbFile))
            {
                Load();
            }
            else
            {
                _data = new JsonData();
                SeedData();
                Save();
            }

            EnsureNotNull();
            Save();
        }

        private static string GetProjectRootDirectory()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                try
                {
                    if (directory.GetFiles("*.csproj").Length > 0)
                    {
                        return directory.FullName;
                    }
                }
                catch
                {
                    break;
                }

                directory = directory.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        }

        private static void EnsureNotNull()
        {
            if (_data == null) _data = new JsonData();
            if (_data.Clients == null) _data.Clients = new List<ClientRecord>();
            if (_data.Suppliers == null) _data.Suppliers = new List<SupplierRecord>();
            if (_data.Materials == null) _data.Materials = new List<MaterialRecord>();
            if (_data.Orders == null) _data.Orders = new List<OrderRecord>();
            if (_data.Designs == null) _data.Designs = new List<DesignRecord>();
            if (_data.ProductionTasks == null) _data.ProductionTasks = new List<ProductionTaskRecord>();
            if (_data.TestProtocols == null) _data.TestProtocols = new List<TestProtocolRecord>();
            if (_data.PackagePayments == null) _data.PackagePayments = new List<PackagePaymentRecord>();
            if (_data.TransportRequests == null) _data.TransportRequests = new List<TransportRequestRecord>();
        }

        private static void Load()
        {
            string text = File.ReadAllText(_dbFile, Encoding.UTF8).TrimStart('\uFEFF').Trim();
            if (String.IsNullOrWhiteSpace(text))
            {
                _data = new JsonData();
                return;
            }

            object rootValue = SimpleJsonParser.Parse(text);
            Dictionary<string, object> root = rootValue as Dictionary<string, object>;
            if (root == null)
            {
                _data = new JsonData();
                return;
            }

            _data = new JsonData();

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "Clients"))
            {
                _data.Clients.Add(new ClientRecord
                {
                    Id = IntValue(item, "Id"),
                    FullName = StringValue(item, "FullName"),
                    Phone = StringValue(item, "Phone"),
                    Email = StringValue(item, "Email"),
                    Address = StringValue(item, "Address")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "Suppliers"))
            {
                _data.Suppliers.Add(new SupplierRecord
                {
                    Id = IntValue(item, "Id"),
                    Name = StringValue(item, "Name"),
                    ContactPerson = StringValue(item, "ContactPerson"),
                    Phone = StringValue(item, "Phone"),
                    Email = StringValue(item, "Email")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "Materials"))
            {
                _data.Materials.Add(new MaterialRecord
                {
                    Id = IntValue(item, "Id"),
                    Name = StringValue(item, "Name"),
                    Unit = StringValue(item, "Unit"),
                    Quantity = DecimalValue(item, "Quantity"),
                    MinQuantity = DecimalValue(item, "MinQuantity"),
                    SupplierId = NullableIntValue(item, "SupplierId")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "Orders"))
            {
                _data.Orders.Add(new OrderRecord
                {
                    Id = IntValue(item, "Id"),
                    ClientId = IntValue(item, "ClientId"),
                    PrinterModel = StringValue(item, "PrinterModel"),
                    Quantity = IntValue(item, "Quantity"),
                    Status = StringValue(item, "Status"),
                    Amount = DecimalValue(item, "Amount"),
                    CreatedAt = DateValue(item, "CreatedAt"),
                    MaterialsConsumed = BoolValue(item, "MaterialsConsumed")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "Designs"))
            {
                _data.Designs.Add(new DesignRecord
                {
                    Id = IntValue(item, "Id"),
                    OrderId = IntValue(item, "OrderId"),
                    DrawingName = StringValue(item, "DrawingName"),
                    Specification = StringValue(item, "Specification"),
                    EngineerName = StringValue(item, "EngineerName"),
                    DesignStatus = StringValueOrDefault(item, "DesignStatus", "В работе"),
                    CreatedAt = DateValue(item, "CreatedAt")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "ProductionTasks"))
            {
                _data.ProductionTasks.Add(new ProductionTaskRecord
                {
                    Id = IntValue(item, "Id"),
                    OrderId = IntValue(item, "OrderId"),
                    OperationName = StringValue(item, "OperationName"),
                    ExecutorName = StringValue(item, "ExecutorName"),
                    Status = StringValue(item, "Status"),
                    StartedAt = DateValue(item, "StartedAt"),
                    FinishedAt = NullableDateValue(item, "FinishedAt")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "TestProtocols"))
            {
                _data.TestProtocols.Add(new TestProtocolRecord
                {
                    Id = IntValue(item, "Id"),
                    OrderId = IntValue(item, "OrderId"),
                    TesterName = StringValue(item, "TesterName"),
                    Result = StringValue(item, "Result"),
                    Comment = StringValue(item, "Comment"),
                    CheckedAt = DateValue(item, "CheckedAt")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "PackagePayments"))
            {
                _data.PackagePayments.Add(new PackagePaymentRecord
                {
                    Id = IntValue(item, "Id"),
                    OrderId = IntValue(item, "OrderId"),
                    PackagerName = StringValue(item, "PackagerName"),
                    DeliveryCode = StringValue(item, "DeliveryCode"),
                    Paid = BoolValue(item, "Paid"),
                    ClosedAt = DateValue(item, "ClosedAt")
                });
            }

            foreach (Dictionary<string, object> item in ReadObjectArray(root, "TransportRequests"))
            {
                _data.TransportRequests.Add(new TransportRequestRecord
                {
                    Id = IntValue(item, "Id"),
                    RequestType = StringValue(item, "RequestType"),
                    OrderId = NullableIntValue(item, "OrderId"),
                    SupplierId = NullableIntValue(item, "SupplierId"),
                    MaterialId = NullableIntValue(item, "MaterialId"),
                    Quantity = DecimalValue(item, "Quantity"),
                    DeliveryCode = StringValue(item, "DeliveryCode"),
                    RequestStatus = StringValue(item, "RequestStatus"),
                    CreatedAt = DateValue(item, "CreatedAt"),
                    CompletedAt = NullableDateValue(item, "CompletedAt"),
                    StockApplied = BoolValue(item, "StockApplied")
                });
            }
        }

        private static IEnumerable<Dictionary<string, object>> ReadObjectArray(Dictionary<string, object> root, string key)
        {
            object value;
            if (!root.TryGetValue(key, out value)) yield break;

            List<object> list = value as List<object>;
            if (list == null) yield break;

            foreach (object item in list)
            {
                Dictionary<string, object> obj = item as Dictionary<string, object>;
                if (obj != null) yield return obj;
            }
        }

        private static string StringValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return String.Empty;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty;
        }

        private static string StringValueOrDefault(Dictionary<string, object> obj, string key, string defaultValue)
        {
            string value = StringValue(obj, key);
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static int IntValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return 0;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static int? NullableIntValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return null;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static decimal DecimalValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return 0m;
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static bool BoolValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return false;
            if (value is bool) return (bool)value;
            return String.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime DateValue(Dictionary<string, object> obj, string key)
        {
            DateTime? value = NullableDateValue(obj, key);
            return value.HasValue ? value.Value : DateTime.Now;
        }

        private static DateTime? NullableDateValue(Dictionary<string, object> obj, string key)
        {
            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return null;
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (String.IsNullOrWhiteSpace(text)) return null;

            DateTime result;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
            {
                return result;
            }
            if (DateTime.TryParse(text, out result))
            {
                return result;
            }
            return null;
        }

        private static void Save()
        {
            EnsureNotNull();
            string tempFile = _dbFile + ".tmp";
            File.WriteAllText(tempFile, SerializeData(), new UTF8Encoding(false));

            if (File.Exists(_dbFile))
            {
                File.Delete(_dbFile);
            }
            File.Move(tempFile, _dbFile);
        }

        private static string SerializeData()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            AppendClients(sb, "  ");
            sb.AppendLine(",");
            AppendSuppliers(sb, "  ");
            sb.AppendLine(",");
            AppendMaterials(sb, "  ");
            sb.AppendLine(",");
            AppendOrders(sb, "  ");
            sb.AppendLine(",");
            AppendDesigns(sb, "  ");
            sb.AppendLine(",");
            AppendProductionTasks(sb, "  ");
            sb.AppendLine(",");
            AppendTestProtocols(sb, "  ");
            sb.AppendLine(",");
            AppendPackagePayments(sb, "  ");
            sb.AppendLine(",");
            AppendTransportRequests(sb, "  ");
            sb.AppendLine();
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendClients(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"Clients\": [");
            for (int i = 0; i < _data.Clients.Count; i++)
            {
                ClientRecord x = _data.Clients[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "FullName", x.FullName, true);
                AppendProp(sb, "Phone", x.Phone, true);
                AppendProp(sb, "Email", x.Email, true);
                AppendProp(sb, "Address", x.Address, false);
                sb.Append("}");
                if (i < _data.Clients.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendSuppliers(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"Suppliers\": [");
            for (int i = 0; i < _data.Suppliers.Count; i++)
            {
                SupplierRecord x = _data.Suppliers[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "Name", x.Name, true);
                AppendProp(sb, "ContactPerson", x.ContactPerson, true);
                AppendProp(sb, "Phone", x.Phone, true);
                AppendProp(sb, "Email", x.Email, false);
                sb.Append("}");
                if (i < _data.Suppliers.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendMaterials(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"Materials\": [");
            for (int i = 0; i < _data.Materials.Count; i++)
            {
                MaterialRecord x = _data.Materials[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "Name", x.Name, true);
                AppendProp(sb, "Unit", x.Unit, true);
                AppendProp(sb, "Quantity", x.Quantity, true);
                AppendProp(sb, "MinQuantity", x.MinQuantity, true);
                AppendProp(sb, "SupplierId", x.SupplierId, false);
                sb.Append("}");
                if (i < _data.Materials.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendOrders(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"Orders\": [");
            for (int i = 0; i < _data.Orders.Count; i++)
            {
                OrderRecord x = _data.Orders[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "ClientId", x.ClientId, true);
                AppendProp(sb, "PrinterModel", x.PrinterModel, true);
                AppendProp(sb, "Quantity", x.Quantity, true);
                AppendProp(sb, "Status", x.Status, true);
                AppendProp(sb, "Amount", x.Amount, true);
                AppendProp(sb, "CreatedAt", DateToString(x.CreatedAt), true);
                AppendProp(sb, "MaterialsConsumed", x.MaterialsConsumed, false);
                sb.Append("}");
                if (i < _data.Orders.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendDesigns(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"Designs\": [");
            for (int i = 0; i < _data.Designs.Count; i++)
            {
                DesignRecord x = _data.Designs[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "OrderId", x.OrderId, true);
                AppendProp(sb, "DrawingName", x.DrawingName, true);
                AppendProp(sb, "Specification", x.Specification, true);
                AppendProp(sb, "EngineerName", x.EngineerName, true);
                AppendProp(sb, "DesignStatus", x.DesignStatus, true);
                AppendProp(sb, "CreatedAt", DateToString(x.CreatedAt), false);
                sb.Append("}");
                if (i < _data.Designs.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendProductionTasks(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"ProductionTasks\": [");
            for (int i = 0; i < _data.ProductionTasks.Count; i++)
            {
                ProductionTaskRecord x = _data.ProductionTasks[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "OrderId", x.OrderId, true);
                AppendProp(sb, "OperationName", x.OperationName, true);
                AppendProp(sb, "ExecutorName", x.ExecutorName, true);
                AppendProp(sb, "Status", x.Status, true);
                AppendProp(sb, "StartedAt", DateToString(x.StartedAt), true);
                AppendProp(sb, "FinishedAt", NullableDateToString(x.FinishedAt), false);
                sb.Append("}");
                if (i < _data.ProductionTasks.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendTestProtocols(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"TestProtocols\": [");
            for (int i = 0; i < _data.TestProtocols.Count; i++)
            {
                TestProtocolRecord x = _data.TestProtocols[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "OrderId", x.OrderId, true);
                AppendProp(sb, "TesterName", x.TesterName, true);
                AppendProp(sb, "Result", x.Result, true);
                AppendProp(sb, "Comment", x.Comment, true);
                AppendProp(sb, "CheckedAt", DateToString(x.CheckedAt), false);
                sb.Append("}");
                if (i < _data.TestProtocols.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendPackagePayments(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"PackagePayments\": [");
            for (int i = 0; i < _data.PackagePayments.Count; i++)
            {
                PackagePaymentRecord x = _data.PackagePayments[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "OrderId", x.OrderId, true);
                AppendProp(sb, "PackagerName", x.PackagerName, true);
                AppendProp(sb, "DeliveryCode", x.DeliveryCode, true);
                AppendProp(sb, "Paid", x.Paid, true);
                AppendProp(sb, "ClosedAt", DateToString(x.ClosedAt), false);
                sb.Append("}");
                if (i < _data.PackagePayments.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static void AppendTransportRequests(StringBuilder sb, string indent)
        {
            sb.Append(indent).AppendLine("\"TransportRequests\": [");
            for (int i = 0; i < _data.TransportRequests.Count; i++)
            {
                TransportRequestRecord x = _data.TransportRequests[i];
                sb.Append(indent).Append("  {");
                AppendProp(sb, "Id", x.Id, true);
                AppendProp(sb, "RequestType", x.RequestType, true);
                AppendProp(sb, "OrderId", x.OrderId, true);
                AppendProp(sb, "SupplierId", x.SupplierId, true);
                AppendProp(sb, "MaterialId", x.MaterialId, true);
                AppendProp(sb, "Quantity", x.Quantity, true);
                AppendProp(sb, "DeliveryCode", x.DeliveryCode, true);
                AppendProp(sb, "RequestStatus", x.RequestStatus, true);
                AppendProp(sb, "CreatedAt", DateToString(x.CreatedAt), true);
                AppendProp(sb, "CompletedAt", NullableDateToString(x.CompletedAt), true);
                AppendProp(sb, "StockApplied", x.StockApplied, false);
                sb.Append("}");
                if (i < _data.TransportRequests.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.Append(indent).Append("]");
        }

        private static string DateToString(DateTime value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string NullableDateToString(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("o", CultureInfo.InvariantCulture) : null;
        }

        private static void AppendProp(StringBuilder sb, string name, string value, bool comma)
        {
            sb.Append("\"").Append(Escape(name)).Append("\":");
            if (value == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append("\"").Append(Escape(value)).Append("\"");
            }
            if (comma) sb.Append(",");
        }

        private static void AppendProp(StringBuilder sb, string name, int value, bool comma)
        {
            sb.Append("\"").Append(Escape(name)).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
            if (comma) sb.Append(",");
        }

        private static void AppendProp(StringBuilder sb, string name, int? value, bool comma)
        {
            sb.Append("\"").Append(Escape(name)).Append("\":");
            if (value.HasValue) sb.Append(value.Value.ToString(CultureInfo.InvariantCulture)); else sb.Append("null");
            if (comma) sb.Append(",");
        }

        private static void AppendProp(StringBuilder sb, string name, decimal value, bool comma)
        {
            sb.Append("\"").Append(Escape(name)).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
            if (comma) sb.Append(",");
        }

        private static void AppendProp(StringBuilder sb, string name, bool value, bool comma)
        {
            sb.Append("\"").Append(Escape(name)).Append("\":").Append(value ? "true" : "false");
            if (comma) sb.Append(",");
        }

        private static string Escape(string value)
        {
            if (value == null) return String.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private static void SeedData()
        {
            EnsureNotNull();
            if (_data.Clients.Count > 0) return;

            _data.Clients.Add(new ClientRecord { Id = 1, FullName = "Иванов Сергей Петрович", Phone = "+7 900 111-22-33", Email = "ivanov@example.ru", Address = "г. Воронеж, ул. Ленина, 10" });
            _data.Clients.Add(new ClientRecord { Id = 2, FullName = "Петрова Анна Викторовна", Phone = "+7 900 555-66-77", Email = "petrova@example.ru", Address = "г. Воронеж, Московский проспект, 41" });

            _data.Suppliers.Add(new SupplierRecord { Id = 1, Name = "Пластик-Комплект", ContactPerson = "Орлов Д. С.", Phone = "+7 473 100-10-10", Email = "plastic@example.ru" });
            _data.Suppliers.Add(new SupplierRecord { Id = 2, Name = "Электроника-Сервис", ContactPerson = "Кузнецова М. А.", Phone = "+7 473 200-20-20", Email = "electro@example.ru" });

            _data.Materials.Add(new MaterialRecord { Id = 1, Name = "PLA-пластик", Unit = "кг", Quantity = 25m, MinQuantity = 5m, SupplierId = 1 });
            _data.Materials.Add(new MaterialRecord { Id = 2, Name = "Алюминиевый профиль", Unit = "м", Quantity = 60m, MinQuantity = 10m, SupplierId = 1 });
            _data.Materials.Add(new MaterialRecord { Id = 3, Name = "Плата управления", Unit = "шт.", Quantity = 15m, MinQuantity = 3m, SupplierId = 2 });
            _data.Materials.Add(new MaterialRecord { Id = 4, Name = "Датчик температуры", Unit = "шт.", Quantity = 30m, MinQuantity = 5m, SupplierId = 2 });

            _data.Orders.Add(new OrderRecord { Id = 1, ClientId = 1, PrinterModel = "FDM Home Mini", Quantity = 1, Status = "Оформлен", Amount = 34900m, CreatedAt = DateTime.Now, MaterialsConsumed = false });
            _data.Orders.Add(new OrderRecord { Id = 2, ClientId = 2, PrinterModel = "FDM Home Pro", Quantity = 1, Status = "Проектирование", Amount = 49900m, CreatedAt = DateTime.Now, MaterialsConsumed = false });

            _data.Designs.Add(new DesignRecord { Id = 1, OrderId = 1, DrawingName = "Чертеж корпуса FDM Home Mini", Specification = "Корпус, направляющие, стол печати, электроника", EngineerName = "Смирнов И. А.", DesignStatus = "В работе", CreatedAt = DateTime.Now });
            _data.ProductionTasks.Add(new ProductionTaskRecord { Id = 1, OrderId = 1, OperationName = "Сборка каркаса", ExecutorName = "Михайлов П. Р.", Status = "В работе", StartedAt = DateTime.Now });
            _data.TestProtocols.Add(new TestProtocolRecord { Id = 1, OrderId = 1, TesterName = "Соколов В. Н.", Result = "Пройдено", Comment = "Точность печати соответствует требованиям", CheckedAt = DateTime.Now });
            _data.PackagePayments.Add(new PackagePaymentRecord { Id = 1, OrderId = 1, PackagerName = "Климова Е. С.", DeliveryCode = "TK-000123", Paid = true, ClosedAt = DateTime.Now });
        }


        public static int Count(string tableName)
        {
            EnsureNotNull();
            if (tableName == "Clients") return _data.Clients.Count;
            if (tableName == "Orders") return _data.Orders.Count;
            if (tableName == "Materials") return _data.Materials.Count;
            if (tableName == "Suppliers") return _data.Suppliers.Count;
            if (tableName == "Designs") return _data.Designs.Count;
            if (tableName == "ProductionTasks") return _data.ProductionTasks.Count;
            if (tableName == "TestProtocols") return _data.TestProtocols.Count;
            if (tableName == "PackagePayments") return _data.PackagePayments.Count;
            if (tableName == "TransportRequests") return _data.TransportRequests.Count;
            if (tableName == "ActiveTransportRequests")
            {
                return _data.TransportRequests.Count(x => !String.Equals(x.RequestStatus, "Доставлено", StringComparison.OrdinalIgnoreCase));
            }
            return 0;
        }

        public static int ScalarInt(string sql)
        {
            EnsureNotNull();
            string s = (sql ?? String.Empty).ToLowerInvariant();
            if (s.IndexOf("from clients") >= 0) return _data.Clients.Count;
            if (s.IndexOf("from suppliers") >= 0) return _data.Suppliers.Count;
            if (s.IndexOf("from materials") >= 0) return _data.Materials.Count;
            if (s.IndexOf("from orders") >= 0) return _data.Orders.Count;
            if (s.IndexOf("from designs") >= 0) return _data.Designs.Count;
            if (s.IndexOf("from productiontasks") >= 0) return _data.ProductionTasks.Count;
            if (s.IndexOf("from testprotocols") >= 0) return _data.TestProtocols.Count;
            if (s.IndexOf("from packagepayments") >= 0) return _data.PackagePayments.Count;
            if (s.IndexOf("from transportrequests") >= 0)
            {
                if (s.IndexOf("доставлено") >= 0 && s.IndexOf("<>") >= 0)
                {
                    return _data.TransportRequests.Count(x => !String.Equals(x.RequestStatus, "Доставлено", StringComparison.OrdinalIgnoreCase));
                }
                return _data.TransportRequests.Count;
            }
            return 0;
        }

        public static DataTable GetClients()
        {
            DataTable table = CreateTable("Id", "ФИО", "Телефон", "Email", "Адрес");
            foreach (ClientRecord x in _data.Clients.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, x.FullName, x.Phone, x.Email, x.Address);
            }
            return table;
        }

        public static DataTable GetSuppliers()
        {
            DataTable table = CreateTable("Id", "Поставщик", "Контактное лицо", "Телефон", "Email");
            foreach (SupplierRecord x in _data.Suppliers.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, x.Name, x.ContactPerson, x.Phone, x.Email);
            }
            return table;
        }

        public static DataTable GetMaterials()
        {
            DataTable table = CreateTable("Id", "Материал", "Ед. изм.", "Остаток", "Мин. остаток", "Статус остатка", "Поставщик");
            foreach (MaterialRecord x in _data.Materials.OrderByDescending(x => x.Id))
            {
                SupplierRecord supplier = _data.Suppliers.FirstOrDefault(s => s.Id == x.SupplierId);
                table.Rows.Add(x.Id, x.Name, x.Unit, x.Quantity, x.MinQuantity, x.Quantity <= x.MinQuantity ? "Нужно пополнить" : "Достаточно", supplier == null ? String.Empty : supplier.Name);
            }
            return table;
        }

        public static DataTable GetOrders()
        {
            DataTable table = CreateTable("Id", "Клиент", "Модель принтера", "Кол-во", "Статус", "Сумма", "Дата");
            foreach (OrderRecord x in _data.Orders.OrderByDescending(x => x.Id))
            {
                ClientRecord client = _data.Clients.FirstOrDefault(c => c.Id == x.ClientId);
                table.Rows.Add(x.Id, client == null ? String.Empty : client.FullName, x.PrinterModel, x.Quantity, x.Status, x.Amount, x.CreatedAt);
            }
            return table;
        }

        public static DataTable GetDesigns()
        {
            DataTable table = CreateTable("Id", "Заказ", "Чертеж", "Спецификация", "Проектировщик", "Статус проектирования", "Дата");
            foreach (DesignRecord x in _data.Designs.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, OrderTitle(x.OrderId), x.DrawingName, x.Specification, x.EngineerName, x.DesignStatus, x.CreatedAt);
            }
            return table;
        }

        public static DataTable GetProductionTasks()
        {
            DataTable table = CreateTable("Id", "Заказ", "Операция", "Исполнитель", "Статус задания", "Начато", "Завершено");
            foreach (ProductionTaskRecord x in _data.ProductionTasks.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, OrderTitle(x.OrderId), x.OperationName, x.ExecutorName, x.Status, x.StartedAt, NullableDateObject(x.FinishedAt));
            }
            return table;
        }

        public static DataTable GetTestProtocols()
        {
            DataTable table = CreateTable("Id", "Заказ", "Тестировщик", "Результат", "Комментарий", "Дата");
            foreach (TestProtocolRecord x in _data.TestProtocols.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, OrderTitle(x.OrderId), x.TesterName, x.Result, x.Comment, x.CheckedAt);
            }
            return table;
        }

        public static DataTable GetPackagePayments()
        {
            DataTable table = CreateTable("Id", "Заказ", "Упаковщик", "Код доставки", "Оплачено", "Дата");
            foreach (PackagePaymentRecord x in _data.PackagePayments.OrderByDescending(x => x.Id))
            {
                table.Rows.Add(x.Id, OrderTitle(x.OrderId), x.PackagerName, x.DeliveryCode, x.Paid ? "Да" : "Нет", x.ClosedAt);
            }
            return table;
        }

        public static DataTable GetTransportRequests()
        {
            DataTable table = CreateTable("Id", "Тип запроса", "Заказ", "Поставщик", "Материал", "Кол-во", "Код заявки", "Статус", "Создано", "Закрыто");
            foreach (TransportRequestRecord x in _data.TransportRequests.OrderByDescending(x => x.Id))
            {
                SupplierRecord supplier = _data.Suppliers.FirstOrDefault(s => s.Id == x.SupplierId);
                MaterialRecord material = _data.Materials.FirstOrDefault(m => m.Id == x.MaterialId);
                table.Rows.Add(x.Id, x.RequestType, x.OrderId.HasValue ? OrderTitle(x.OrderId.Value) : String.Empty, supplier == null ? String.Empty : supplier.Name, material == null ? String.Empty : material.Name, x.Quantity, x.DeliveryCode, x.RequestStatus, x.CreatedAt, NullableDateObject(x.CompletedAt));
            }
            return table;
        }

        public static DataTable GetClientLookup()
        {
            DataTable table = CreateTable("Id", "FullName");
            foreach (ClientRecord x in _data.Clients.OrderBy(x => x.FullName)) table.Rows.Add(x.Id, x.FullName);
            return table;
        }

        public static DataTable GetSupplierLookup()
        {
            DataTable table = CreateTable("Id", "Name");
            foreach (SupplierRecord x in _data.Suppliers.OrderBy(x => x.Name)) table.Rows.Add(x.Id, x.Name);
            return table;
        }

        public static DataTable GetMaterialLookup()
        {
            DataTable table = CreateTable("Id", "Name");
            foreach (MaterialRecord x in _data.Materials.OrderBy(x => x.Name)) table.Rows.Add(x.Id, x.Name);
            return table;
        }

        public static DataTable GetOrderLookup()
        {
            DataTable table = CreateTable("Id", "Title");
            foreach (OrderRecord x in _data.Orders.OrderByDescending(x => x.Id)) table.Rows.Add(x.Id, OrderTitle(x.Id));
            return table;
        }

        private static DataTable CreateTable(params string[] columns)
        {
            DataTable table = new DataTable();
            for (int i = 0; i < columns.Length; i++) table.Columns.Add(columns[i]);
            return table;
        }

        private static object NullableDateObject(DateTime? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static string OrderTitle(int orderId)
        {
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return "№ " + orderId.ToString(CultureInfo.InvariantCulture) + " — заказ удален";
            ClientRecord client = _data.Clients.FirstOrDefault(c => c.Id == order.ClientId);
            string clientName = client == null ? "клиент удален" : client.FullName;
            return "№ " + order.Id.ToString(CultureInfo.InvariantCulture) + " — " + clientName + " — " + order.PrinterModel + " — " + order.Status;
        }

        private static int NextId<T>(IEnumerable<T> list, Func<T, int> selector)
        {
            if (list == null || !list.Any()) return 1;
            return list.Max(selector) + 1;
        }


        public static DataTable GetStockCheckForOrder(int orderId)
        {
            EnsureNotNull();
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new Exception("Заказ не найден.");

            DataTable table = CreateTable("Материал", "Нужно", "Есть на складе", "Мин. остаток", "После производства", "Статус");
            foreach (MaterialNeed need in BuildMaterialNeeds(order))
            {
                object available = need.Material == null ? (object)"0" : need.Material.Quantity;
                object min = need.Material == null ? (object)"-" : need.Material.MinQuantity;
                object after = need.Material == null ? (object)"-" : (object)(need.Material.Quantity - need.RequiredQuantity);
                string status;
                if (need.Material == null) status = "Материал не найден";
                else if (need.Shortage > 0) status = "Не хватает: " + need.Shortage.ToString(CultureInfo.InvariantCulture);
                else if (need.Material.Quantity - need.RequiredQuantity <= need.Material.MinQuantity) status = "Хватает, но после производства ниже мин. остатка";
                else status = "Хватает";
                table.Rows.Add(need.MaterialName, need.RequiredQuantity, available, min, after, status);
            }
            return table;
        }

        public static string CheckStockAndRouteOrder(int orderId)
        {
            EnsureNotNull();
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new Exception("Заказ не найден.");

            List<MaterialNeed> needs = BuildMaterialNeeds(order);
            List<MaterialNeed> missing = needs.Where(n => n.Shortage > 0).ToList();

            if (missing.Count == 0)
            {
                EnsureStandardProductionTasks(orderId);
                UpdateOrderStatusInternal(orderId, "Производство");
                Save();
                return "Склад проверен бухгалтером. Материалов хватает, заказ переведен в производство. Списание материалов произойдет после завершения производственных заданий.";
            }

            UpdateOrderStatusInternal(orderId, "Закупка материалов");
            Save();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Склад проверен бухгалтером. Материалов не хватает, заказ переведен на этап закупки материалов.");
            sb.AppendLine();
            sb.AppendLine("Нехватка:");
            foreach (MaterialNeed need in missing)
            {
                sb.AppendLine("- " + need.MaterialName + ": нужно " + need.RequiredQuantity.ToString(CultureInfo.InvariantCulture) + ", есть " + need.AvailableQuantity.ToString(CultureInfo.InvariantCulture) + ", не хватает " + need.Shortage.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static void EnsureStandardProductionTasks(int orderId)
        {
            string[] operations = new string[]
            {
                "Производство пластиковых элементов",
                "Производство металлических элементов",
                "Производство электронных деталей",
                "Сборка принтера",
                "Калибровка продукта"
            };

            foreach (string operation in operations)
            {
                bool exists = _data.ProductionTasks.Any(t => t.OrderId == orderId && String.Equals(t.OperationName, operation, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    _data.ProductionTasks.Add(new ProductionTaskRecord
                    {
                        Id = NextId(_data.ProductionTasks, x => x.Id),
                        OrderId = orderId,
                        OperationName = operation,
                        ExecutorName = "Производственный участок",
                        Status = "Запланировано",
                        StartedAt = DateTime.Now,
                        FinishedAt = null
                    });
                }
            }
        }

        private static void ConsumeMaterialsForOrder(int orderId)
        {
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new Exception("Заказ не найден.");
            if (order.MaterialsConsumed) return;

            List<MaterialNeed> needs = BuildMaterialNeeds(order);
            List<MaterialNeed> missing = needs.Where(n => n.Shortage > 0).ToList();
            if (missing.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Нельзя завершить производство: на складе не хватает материалов.");
                foreach (MaterialNeed need in missing)
                {
                    sb.AppendLine("- " + need.MaterialName + ": не хватает " + need.Shortage.ToString(CultureInfo.InvariantCulture));
                }
                throw new Exception(sb.ToString());
            }

            foreach (MaterialNeed need in needs)
            {
                if (need.Material != null)
                {
                    need.Material.Quantity -= need.RequiredQuantity;
                }
            }
            order.MaterialsConsumed = true;
        }

        private static List<MaterialNeed> BuildMaterialNeeds(OrderRecord order)
        {
            decimal pla = 1m;
            decimal profile = 2m;
            decimal board = 1m;
            decimal sensor = 1m;

            string model = order.PrinterModel ?? String.Empty;
            if (model.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pla = 1.2m; profile = 2.5m; board = 1m; sensor = 1m;
            }
            else if (model.IndexOf("Pro Plus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pla = 2m; profile = 3.5m; board = 1m; sensor = 2m;
            }
            else if (model.IndexOf("Pro", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pla = 1.6m; profile = 3m; board = 1m; sensor = 2m;
            }

            int quantity = order.Quantity <= 0 ? 1 : order.Quantity;
            List<MaterialNeed> result = new List<MaterialNeed>();
            result.Add(CreateNeed("PLA-пластик", pla * quantity));
            result.Add(CreateNeed("Алюминиевый профиль", profile * quantity));
            result.Add(CreateNeed("Плата управления", board * quantity));
            result.Add(CreateNeed("Датчик температуры", sensor * quantity));
            return result;
        }

        private static MaterialNeed CreateNeed(string materialName, decimal requiredQuantity)
        {
            MaterialRecord material = FindMaterialByName(materialName);
            decimal available = material == null ? 0m : material.Quantity;
            return new MaterialNeed
            {
                MaterialName = materialName,
                RequiredQuantity = requiredQuantity,
                AvailableQuantity = available,
                Material = material
            };
        }

        private static MaterialRecord FindMaterialByName(string materialName)
        {
            return _data.Materials.FirstOrDefault(m => String.Equals((m.Name ?? String.Empty).Trim(), materialName, StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyMaterialDeliveryToStock(TransportRequestRecord request)
        {
            if (request.StockApplied) return;
            if (!request.MaterialId.HasValue) return;
            if (request.Quantity <= 0) return;

            MaterialRecord material = _data.Materials.FirstOrDefault(m => m.Id == request.MaterialId.Value);
            if (material == null) throw new Exception("Материал для доставки не найден на складе.");
            material.Quantity += request.Quantity;
            request.StockApplied = true;
        }

        private class MaterialNeed
        {
            public string MaterialName;
            public decimal RequiredQuantity;
            public decimal AvailableQuantity;
            public MaterialRecord Material;
            public decimal Shortage { get { return RequiredQuantity > AvailableQuantity ? RequiredQuantity - AvailableQuantity : 0m; } }
        }

        public static int GetOrCreateClient(string fullName, string phone, string email, string address)
        {
            EnsureNotNull();
            fullName = (fullName ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(fullName)) throw new Exception("Введите ФИО клиента.");

            ClientRecord existing = _data.Clients.FirstOrDefault(c => String.Equals((c.FullName ?? String.Empty).Trim(), fullName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing.Id;

            ClientRecord client = new ClientRecord
            {
                Id = NextId(_data.Clients, x => x.Id),
                FullName = fullName,
                Phone = phone ?? String.Empty,
                Email = email ?? String.Empty,
                Address = address ?? String.Empty
            };
            _data.Clients.Add(client);
            Save();
            return client.Id;
        }

        public static void AddClient(string fullName, string phone, string email, string address)
        {
            GetOrCreateClient(fullName, phone, email, address);
        }

        public static void AddSupplier(string name, string contact, string phone, string email)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new Exception("Введите название поставщика.");
            _data.Suppliers.Add(new SupplierRecord
            {
                Id = NextId(_data.Suppliers, x => x.Id),
                Name = name,
                ContactPerson = contact ?? String.Empty,
                Phone = phone ?? String.Empty,
                Email = email ?? String.Empty
            });
            Save();
        }

        public static void AddMaterial(string name, string unit, decimal quantity, decimal minQuantity, object supplierId)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new Exception("Введите название материала.");
            int? supplier = null;
            if (supplierId != null && supplierId != DBNull.Value) supplier = Convert.ToInt32(supplierId, CultureInfo.InvariantCulture);

            _data.Materials.Add(new MaterialRecord
            {
                Id = NextId(_data.Materials, x => x.Id),
                Name = name,
                Unit = String.IsNullOrWhiteSpace(unit) ? "шт." : unit,
                Quantity = quantity,
                MinQuantity = minQuantity,
                SupplierId = supplier
            });
            Save();
        }

        public static void AddOrder(int clientId, string printerModel, int quantity, string status, decimal amount)
        {
            if (_data.Clients.FirstOrDefault(c => c.Id == clientId) == null) throw new Exception("Клиент не найден.");
            _data.Orders.Add(new OrderRecord
            {
                Id = NextId(_data.Orders, x => x.Id),
                ClientId = clientId,
                PrinterModel = printerModel,
                Quantity = quantity,
                Status = status,
                Amount = amount,
                CreatedAt = DateTime.Now,
                MaterialsConsumed = false
            });
            Save();
        }

        public static void AddOrderWithClient(string clientName, string phone, string email, string address, string printerModel, int quantity, decimal unitPrice)
        {
            int clientId = GetOrCreateClient(clientName, phone, email, address);
            decimal amount = unitPrice * quantity;
            AddOrder(clientId, printerModel, quantity, "Оформлен", amount);
        }

        public static void AddDesign(int orderId, string drawingName, string specification, string engineerName)
        {
            SendOrderToDesign(orderId);
            DesignRecord design = _data.Designs.Where(d => d.OrderId == orderId).OrderByDescending(d => d.Id).FirstOrDefault();
            if (design == null) throw new Exception("Не удалось создать запись проектирования.");
            design.DrawingName = drawingName;
            design.Specification = specification;
            design.EngineerName = engineerName;
            design.DesignStatus = "В работе";
            Save();
        }

        public static void SendOrderToDesign(int orderId)
        {
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new Exception("Заказ не найден.");

            DesignRecord existing = _data.Designs.Where(d => d.OrderId == orderId).OrderByDescending(d => d.Id).FirstOrDefault();
            if (existing == null)
            {
                _data.Designs.Add(new DesignRecord
                {
                    Id = NextId(_data.Designs, x => x.Id),
                    OrderId = orderId,
                    DrawingName = String.Empty,
                    Specification = String.Empty,
                    EngineerName = String.Empty,
                    DesignStatus = "Ожидает проектирования",
                    CreatedAt = DateTime.Now
                });
            }
            else if (String.IsNullOrWhiteSpace(existing.DesignStatus) || String.Equals(existing.DesignStatus, "Пройдено", StringComparison.OrdinalIgnoreCase) == false)
            {
                existing.DesignStatus = String.IsNullOrWhiteSpace(existing.DesignStatus) ? "Ожидает проектирования" : existing.DesignStatus;
            }

            UpdateOrderStatusInternal(orderId, "Проектирование");
            Save();
        }

        public static void UpdateDesign(int designId, string drawingName, string specification, string engineerName)
        {
            DesignRecord design = _data.Designs.FirstOrDefault(d => d.Id == designId);
            if (design == null) throw new Exception("Запись проектирования не найдена.");
            design.DrawingName = drawingName;
            design.Specification = specification;
            design.EngineerName = engineerName;
            if (!String.Equals(design.DesignStatus, "Пройдено", StringComparison.OrdinalIgnoreCase))
            {
                design.DesignStatus = "В работе";
            }
            Save();
        }

        public static void PassDesign(int designId)
        {
            DesignRecord design = _data.Designs.FirstOrDefault(d => d.Id == designId);
            if (design == null) throw new Exception("Запись проектирования не найдена.");
            design.DesignStatus = "Пройдено";
            UpdateOrderStatusInternal(design.OrderId, "Проверка склада");
            Save();
        }

        public static void PassDesignByOrder(int orderId)
        {
            DesignRecord design = _data.Designs.Where(d => d.OrderId == orderId).OrderByDescending(d => d.Id).FirstOrDefault();
            if (design == null)
            {
                SendOrderToDesign(orderId);
                design = _data.Designs.Where(d => d.OrderId == orderId).OrderByDescending(d => d.Id).FirstOrDefault();
            }
            if (design == null) throw new Exception("Для выбранного заказа не удалось создать запись проектирования.");
            PassDesign(design.Id);
        }





        public static void AddProductionTask(int orderId, string operationName, string executorName, string status)
        {
            if (_data.Orders.FirstOrDefault(o => o.Id == orderId) == null) throw new Exception("Заказ не найден.");
            _data.ProductionTasks.Add(new ProductionTaskRecord
            {
                Id = NextId(_data.ProductionTasks, x => x.Id),
                OrderId = orderId,
                OperationName = operationName,
                ExecutorName = executorName,
                Status = status,
                StartedAt = DateTime.Now,
                FinishedAt = String.Equals(status, "Завершено", StringComparison.OrdinalIgnoreCase) ? (DateTime?)DateTime.Now : null
            });
            RouteOrderAfterProductionChange(orderId);
            Save();
        }

        public static void FinishProductionTask(int taskId)
        {
            UpdateProductionTaskStatus(taskId, "Завершено");
        }

        public static void UpdateProductionTaskStatus(int taskId, string status)
        {
            UpdateProductionTask(taskId, null, status);
        }

        public static void UpdateProductionTask(int taskId, string executorName, string status)
        {
            ProductionTaskRecord task = _data.ProductionTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) throw new Exception("Производственное задание не найдено.");

            if (executorName != null) task.ExecutorName = executorName;
            task.Status = status;
            task.FinishedAt = String.Equals(status, "Завершено", StringComparison.OrdinalIgnoreCase) ? (DateTime?)DateTime.Now : null;

            RouteOrderAfterProductionChange(task.OrderId);
            Save();
        }

        private static void RouteOrderAfterProductionChange(int orderId)
        {
            List<ProductionTaskRecord> orderTasks = _data.ProductionTasks.Where(t => t.OrderId == orderId).ToList();
            bool allFinished = orderTasks.Count > 0 && orderTasks.All(t => String.Equals(t.Status, "Завершено", StringComparison.OrdinalIgnoreCase));
            if (allFinished)
            {
                ConsumeMaterialsForOrder(orderId);
                EnsureTestProtocolForOrder(orderId);
                UpdateOrderStatusInternal(orderId, "Тестирование");
            }
            else
            {
                UpdateOrderStatusInternal(orderId, "Производство");
            }
        }

        public static void AddTestProtocol(int orderId, string testerName, string result, string comment)
        {
            if (_data.Orders.FirstOrDefault(o => o.Id == orderId) == null) throw new Exception("Заказ не найден.");
            TestProtocolRecord protocol = EnsureTestProtocolForOrder(orderId);
            protocol.TesterName = testerName;
            protocol.Result = result;
            protocol.Comment = comment;
            protocol.CheckedAt = DateTime.Now;
            RouteOrderAfterTestResult(protocol.OrderId, result);
            Save();
        }

        public static void PassTestProtocol(int testProtocolId)
        {
            UpdateTestProtocol(testProtocolId, null, "Пройдено", null);
        }

        public static void UpdateTestProtocolResult(int testProtocolId, string result, string comment)
        {
            UpdateTestProtocol(testProtocolId, null, result, comment);
        }

        public static void UpdateTestProtocol(int testProtocolId, string testerName, string result, string comment)
        {
            TestProtocolRecord protocol = _data.TestProtocols.FirstOrDefault(t => t.Id == testProtocolId);
            if (protocol == null) throw new Exception("Протокол тестирования не найден.");
            if (testerName != null) protocol.TesterName = testerName;
            protocol.Result = result;
            if (comment != null) protocol.Comment = comment;
            protocol.CheckedAt = DateTime.Now;
            RouteOrderAfterTestResult(protocol.OrderId, result);
            Save();
        }

        private static TestProtocolRecord EnsureTestProtocolForOrder(int orderId)
        {
            TestProtocolRecord existing = _data.TestProtocols.Where(t => t.OrderId == orderId).OrderByDescending(t => t.Id).FirstOrDefault();
            if (existing != null) return existing;

            TestProtocolRecord protocol = new TestProtocolRecord
            {
                Id = NextId(_data.TestProtocols, x => x.Id),
                OrderId = orderId,
                TesterName = String.Empty,
                Result = "Ожидает проверки",
                Comment = String.Empty,
                CheckedAt = DateTime.Now
            };
            _data.TestProtocols.Add(protocol);
            return protocol;
        }

        private static void RouteOrderAfterTestResult(int orderId, string result)
        {
            if (String.Equals(result, "Пройдено", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePackagePaymentForOrder(orderId);
                UpdateOrderStatusInternal(orderId, "Упаковка");
            }
            else if (String.Equals(result, "Требуется доработка", StringComparison.OrdinalIgnoreCase) || String.Equals(result, "Не пройдено", StringComparison.OrdinalIgnoreCase))
            {
                UpdateOrderStatusInternal(orderId, "Производство");
            }
            else
            {
                UpdateOrderStatusInternal(orderId, "Тестирование");
            }
        }





        public static void AddPackagePayment(int orderId, string packagerName, string deliveryCode, bool paid)
        {
            if (_data.Orders.FirstOrDefault(o => o.Id == orderId) == null) throw new Exception("Заказ не найден.");
            PackagePaymentRecord payment = EnsurePackagePaymentForOrder(orderId);
            payment.PackagerName = packagerName;
            payment.DeliveryCode = deliveryCode;
            payment.Paid = paid;
            payment.ClosedAt = DateTime.Now;
            UpdateOrderStatusInternal(orderId, paid ? "Оплачен" : "Упаковка");
            Save();
        }

        public static void UpdatePackagePayment(int packagePaymentId, string packagerName, string deliveryCode, bool paid)
        {
            PackagePaymentRecord payment = _data.PackagePayments.FirstOrDefault(p => p.Id == packagePaymentId);
            if (payment == null) throw new Exception("Запись упаковки и оплаты не найдена.");
            payment.PackagerName = packagerName;
            payment.DeliveryCode = deliveryCode;
            payment.Paid = paid;
            payment.ClosedAt = DateTime.Now;
            UpdateOrderStatusInternal(payment.OrderId, paid ? "Оплачен" : "Упаковка");
            Save();
        }

        private static PackagePaymentRecord EnsurePackagePaymentForOrder(int orderId)
        {
            PackagePaymentRecord existing = _data.PackagePayments.Where(p => p.OrderId == orderId).OrderByDescending(p => p.Id).FirstOrDefault();
            if (existing != null) return existing;

            PackagePaymentRecord payment = new PackagePaymentRecord
            {
                Id = NextId(_data.PackagePayments, x => x.Id),
                OrderId = orderId,
                PackagerName = String.Empty,
                DeliveryCode = "TK-",
                Paid = false,
                ClosedAt = DateTime.Now
            };
            _data.PackagePayments.Add(payment);
            return payment;
        }

        public static void SetPackagePaymentPaid(int packagePaymentId, bool paid)
        {
            PackagePaymentRecord payment = _data.PackagePayments.FirstOrDefault(p => p.Id == packagePaymentId);
            if (payment == null) throw new Exception("Запись упаковки и оплаты не найдена.");
            payment.Paid = paid;
            payment.ClosedAt = DateTime.Now;
            UpdateOrderStatusInternal(payment.OrderId, paid ? "Оплачен" : "Упаковка");
            Save();
        }

        public static void AddPrinterTransportRequestFromPackagePayment(int packagePaymentId)
        {
            PackagePaymentRecord payment = _data.PackagePayments.FirstOrDefault(p => p.Id == packagePaymentId);
            if (payment == null) throw new Exception("Запись упаковки и оплаты не найдена.");
            if (!payment.Paid) throw new Exception("Нельзя передать принтер транспортной компании, пока заказ не отмечен как оплаченный.");

            bool hasActive = _data.TransportRequests.Any(r => r.OrderId == payment.OrderId && String.Equals(r.RequestType, "Доставка принтера клиенту", StringComparison.OrdinalIgnoreCase) && !String.Equals(r.RequestStatus, "Доставлено", StringComparison.OrdinalIgnoreCase));
            if (!hasActive)
            {
                _data.TransportRequests.Add(new TransportRequestRecord
                {
                    Id = NextId(_data.TransportRequests, x => x.Id),
                    RequestType = "Доставка принтера клиенту",
                    OrderId = payment.OrderId,
                    SupplierId = null,
                    MaterialId = null,
                    Quantity = 0m,
                    DeliveryCode = payment.DeliveryCode,
                    RequestStatus = "Заявка передана в транспортную компанию",
                    CreatedAt = DateTime.Now,
                    CompletedAt = null,
                    StockApplied = false
                });
            }

            _data.PackagePayments.Remove(payment);
            UpdateOrderStatusInternal(payment.OrderId, "Транспортная компания");
            Save();
        }

        public static void AddDeliveryFromPackagePayment(int packagePaymentId)
        {
            AddPrinterTransportRequestFromPackagePayment(packagePaymentId);
        }

        public static void AddTransportRequest(string requestType, int? orderId, int? supplierId, int? materialId, decimal quantity, string deliveryCode, string status)
        {
            if (String.IsNullOrWhiteSpace(requestType)) throw new Exception("Выберите тип запроса.");

            bool printerDelivery = IsPrinterDelivery(requestType);
            bool materialDelivery = IsMaterialDelivery(requestType);

            if (printerDelivery && !orderId.HasValue) throw new Exception("Для доставки принтера клиенту выберите заказ.");
            if (materialDelivery && !orderId.HasValue) throw new Exception("Для доставки материалов выберите заказ, чтобы статус появился во вкладке Производство.");
            if (materialDelivery && !supplierId.HasValue) throw new Exception("Для доставки материалов выберите поставщика.");
            if (materialDelivery && !materialId.HasValue) throw new Exception("Для доставки материалов выберите материал.");
            if (materialDelivery && quantity <= 0) throw new Exception("Для доставки материалов укажите количество больше нуля.");

            if (printerDelivery)
            {
                supplierId = null;
                materialId = null;
                quantity = 0m;
            }

            TransportRequestRecord request = new TransportRequestRecord
            {
                Id = NextId(_data.TransportRequests, x => x.Id),
                RequestType = requestType,
                OrderId = orderId,
                SupplierId = supplierId,
                MaterialId = materialId,
                Quantity = quantity,
                DeliveryCode = deliveryCode,
                RequestStatus = status,
                CreatedAt = DateTime.Now,
                CompletedAt = String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase) ? (DateTime?)DateTime.Now : null,
                StockApplied = false
            };

            _data.TransportRequests.Add(request);

            if (printerDelivery && orderId.HasValue)
            {
                UpdateOrderStatusInternal(orderId.Value, String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase) ? "Завершен" : "Транспортная компания");
            }
            else if (materialDelivery && orderId.HasValue)
            {
                if (String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyMaterialDeliveryToStock(request);
                    RegisterMaterialDeliveryInProduction(request);
                }
                else
                {
                    UpdateOrderStatusInternal(orderId.Value, "Закупка материалов");
                }
            }

            Save();
        }

        public static void UpdateTransportRequestStatus(int requestId, string status)
        {
            TransportRequestRecord request = _data.TransportRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null) throw new Exception("Запрос в транспортную компанию не найден.");

            request.RequestStatus = status;
            request.CompletedAt = String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase) ? (DateTime?)DateTime.Now : null;

            if (request.OrderId.HasValue && IsPrinterDelivery(request.RequestType))
            {
                UpdateOrderStatusInternal(request.OrderId.Value, String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase) ? "Завершен" : "Транспортная компания");
            }
            else if (IsMaterialDelivery(request.RequestType))
            {
                if (!request.OrderId.HasValue)
                {
                    if (String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("У этой доставки материалов не выбран заказ. Создайте новую заявку с выбранным заказом, иначе запись не может появиться во вкладке Производство.");
                    }
                }
                else if (String.Equals(status, "Доставлено", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyMaterialDeliveryToStock(request);
                    RegisterMaterialDeliveryInProduction(request);
                }
                else
                {
                    UpdateOrderStatusInternal(request.OrderId.Value, "Закупка материалов");
                }
            }
            Save();
        }

        public static void CompleteTransportRequest(int requestId)
        {
            UpdateTransportRequestStatus(requestId, "Доставлено");
        }

        private static bool IsPrinterDelivery(string requestType)
        {
            return !String.IsNullOrWhiteSpace(requestType) && requestType.IndexOf("принтера", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMaterialDelivery(string requestType)
        {
            return !String.IsNullOrWhiteSpace(requestType) && requestType.IndexOf("материал", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RegisterMaterialDeliveryInProduction(TransportRequestRecord request)
        {
            if (!request.OrderId.HasValue) return;

            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == request.OrderId.Value);
            if (order == null) throw new Exception("Заказ, связанный с доставкой материалов, не найден.");

            SupplierRecord supplier = request.SupplierId.HasValue ? _data.Suppliers.FirstOrDefault(s => s.Id == request.SupplierId.Value) : null;
            MaterialRecord material = request.MaterialId.HasValue ? _data.Materials.FirstOrDefault(m => m.Id == request.MaterialId.Value) : null;

            string materialName = material == null ? "материалы" : material.Name;
            string supplierName = supplier == null ? "поставщик не указан" : supplier.Name;
            string code = String.IsNullOrWhiteSpace(request.DeliveryCode) ? "без кода" : request.DeliveryCode;
            string operationName = "Доставка материалов от поставщика: " + materialName + " / " + supplierName + " / заявка " + code;

            ProductionTaskRecord task = _data.ProductionTasks.FirstOrDefault(t =>
                t.OrderId == request.OrderId.Value &&
                String.Equals(t.OperationName, operationName, StringComparison.OrdinalIgnoreCase));

            if (task == null)
            {
                _data.ProductionTasks.Add(new ProductionTaskRecord
                {
                    Id = NextId(_data.ProductionTasks, x => x.Id),
                    OrderId = request.OrderId.Value,
                    OperationName = operationName,
                    ExecutorName = "Транспортная компания",
                    Status = "Завершено",
                    StartedAt = request.CreatedAt,
                    FinishedAt = DateTime.Now
                });
            }
            else
            {
                task.ExecutorName = "Транспортная компания";
                task.Status = "Завершено";
                task.FinishedAt = DateTime.Now;
            }

            OrderRecord linkedOrder = _data.Orders.FirstOrDefault(o => o.Id == request.OrderId.Value);
            if (linkedOrder != null && BuildMaterialNeeds(linkedOrder).All(n => n.Shortage <= 0))
            {
                EnsureStandardProductionTasks(request.OrderId.Value);
                UpdateOrderStatusInternal(request.OrderId.Value, "Производство");
            }
            else
            {
                UpdateOrderStatusInternal(request.OrderId.Value, "Закупка материалов");
            }
        }

        public static void UpdateOrderStatus(int orderId, string status)
        {
            UpdateOrderStatusInternal(orderId, status);
            Save();
        }

        private static void UpdateOrderStatusInternal(int orderId, string status)
        {
            OrderRecord order = _data.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) throw new Exception("Заказ не найден.");
            order.Status = status;
        }

        public static void UpdateMaterialQuantity(int materialId, decimal quantity)
        {
            MaterialRecord material = _data.Materials.FirstOrDefault(m => m.Id == materialId);
            if (material == null) throw new Exception("Материал не найден.");
            material.Quantity = quantity;
            Save();
        }

        public static void DeleteById(string tableName, int id)
        {
            if (tableName == "Clients")
            {
                DeleteClientWithDependencies(id);
                Save();
                return;
            }
            if (tableName == "Orders")
            {
                DeleteOrderWithDependencies(id);
                Save();
                return;
            }
            if (tableName == "Suppliers")
            {
                foreach (MaterialRecord m in _data.Materials.Where(m => m.SupplierId == id)) m.SupplierId = null;
                foreach (TransportRequestRecord r in _data.TransportRequests.Where(r => r.SupplierId == id)) r.SupplierId = null;
                _data.Suppliers.RemoveAll(s => s.Id == id);
                Save();
                return;
            }
            if (tableName == "Materials")
            {
                foreach (TransportRequestRecord r in _data.TransportRequests.Where(r => r.MaterialId == id)) r.MaterialId = null;
                _data.Materials.RemoveAll(m => m.Id == id);
                Save();
                return;
            }
            if (tableName == "Designs") _data.Designs.RemoveAll(x => x.Id == id);
            else if (tableName == "ProductionTasks") _data.ProductionTasks.RemoveAll(x => x.Id == id);
            else if (tableName == "TestProtocols") _data.TestProtocols.RemoveAll(x => x.Id == id);
            else if (tableName == "PackagePayments") _data.PackagePayments.RemoveAll(x => x.Id == id);
            else if (tableName == "TransportRequests") _data.TransportRequests.RemoveAll(x => x.Id == id);
            else throw new InvalidOperationException("Недопустимое имя таблицы.");
            Save();
        }

        private static void DeleteOrderWithDependencies(int orderId)
        {
            _data.TransportRequests.RemoveAll(r => r.OrderId == orderId);
            _data.PackagePayments.RemoveAll(p => p.OrderId == orderId);
            _data.TestProtocols.RemoveAll(t => t.OrderId == orderId);
            _data.ProductionTasks.RemoveAll(p => p.OrderId == orderId);
            _data.Designs.RemoveAll(d => d.OrderId == orderId);
            _data.Orders.RemoveAll(o => o.Id == orderId);
        }

        private static void DeleteClientWithDependencies(int clientId)
        {
            List<int> orderIds = _data.Orders.Where(o => o.ClientId == clientId).Select(o => o.Id).ToList();
            foreach (int orderId in orderIds) DeleteOrderWithDependencies(orderId);
            _data.Clients.RemoveAll(c => c.Id == clientId);
        }

        private class JsonData
        {
            public List<ClientRecord> Clients = new List<ClientRecord>();
            public List<SupplierRecord> Suppliers = new List<SupplierRecord>();
            public List<MaterialRecord> Materials = new List<MaterialRecord>();
            public List<OrderRecord> Orders = new List<OrderRecord>();
            public List<DesignRecord> Designs = new List<DesignRecord>();
            public List<ProductionTaskRecord> ProductionTasks = new List<ProductionTaskRecord>();
            public List<TestProtocolRecord> TestProtocols = new List<TestProtocolRecord>();
            public List<PackagePaymentRecord> PackagePayments = new List<PackagePaymentRecord>();
            public List<TransportRequestRecord> TransportRequests = new List<TransportRequestRecord>();
        }

        private class ClientRecord { public int Id; public string FullName; public string Phone; public string Email; public string Address; }
        private class SupplierRecord { public int Id; public string Name; public string ContactPerson; public string Phone; public string Email; }
        private class MaterialRecord { public int Id; public string Name; public string Unit; public decimal Quantity; public decimal MinQuantity; public int? SupplierId; }
        private class OrderRecord { public int Id; public int ClientId; public string PrinterModel; public int Quantity; public string Status; public decimal Amount; public DateTime CreatedAt; public bool MaterialsConsumed; }
        private class DesignRecord { public int Id; public int OrderId; public string DrawingName; public string Specification; public string EngineerName; public string DesignStatus; public DateTime CreatedAt; }
        private class ProductionTaskRecord { public int Id; public int OrderId; public string OperationName; public string ExecutorName; public string Status; public DateTime StartedAt; public DateTime? FinishedAt; }
        private class TestProtocolRecord { public int Id; public int OrderId; public string TesterName; public string Result; public string Comment; public DateTime CheckedAt; }
        private class PackagePaymentRecord { public int Id; public int OrderId; public string PackagerName; public string DeliveryCode; public bool Paid; public DateTime ClosedAt; }
        private class TransportRequestRecord { public int Id; public string RequestType; public int? OrderId; public int? SupplierId; public int? MaterialId; public decimal Quantity; public string DeliveryCode; public string RequestStatus; public DateTime CreatedAt; public DateTime? CompletedAt; public bool StockApplied; }
    }

    internal class SimpleJsonParser
    {
        private readonly string _text;
        private int _pos;

        private SimpleJsonParser(string text)
        {
            _text = text ?? String.Empty;
            _pos = 0;
        }

        public static object Parse(string text)
        {
            SimpleJsonParser parser = new SimpleJsonParser(text);
            return parser.ParseValue();
        }

        private object ParseValue()
        {
            SkipWhiteSpace();
            if (_pos >= _text.Length) return null;
            char c = _text[_pos];
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (c == '"') return ParseString();
            if (c == 't') { Expect("true"); return true; }
            if (c == 'f') { Expect("false"); return false; }
            if (c == 'n') { Expect("null"); return null; }
            return ParseNumber();
        }

        private Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            Expect('{');
            SkipWhiteSpace();
            if (Peek('}')) { _pos++; return obj; }

            while (true)
            {
                SkipWhiteSpace();
                string key = ParseString();
                SkipWhiteSpace();
                Expect(':');
                object value = ParseValue();
                obj[key] = value;
                SkipWhiteSpace();
                if (Peek('}')) { _pos++; break; }
                Expect(',');
            }
            return obj;
        }

        private List<object> ParseArray()
        {
            List<object> list = new List<object>();
            Expect('[');
            SkipWhiteSpace();
            if (Peek(']')) { _pos++; return list; }

            while (true)
            {
                list.Add(ParseValue());
                SkipWhiteSpace();
                if (Peek(']')) { _pos++; break; }
                Expect(',');
            }
            return list;
        }

        private string ParseString()
        {
            Expect('"');
            StringBuilder sb = new StringBuilder();
            while (_pos < _text.Length)
            {
                char c = _text[_pos++];
                if (c == '"') break;
                if (c == '\\')
                {
                    if (_pos >= _text.Length) break;
                    char esc = _text[_pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            string hex = _text.Substring(_pos, 4);
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _pos += 4;
                            break;
                        default:
                            sb.Append(esc);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private object ParseNumber()
        {
            int start = _pos;
            while (_pos < _text.Length)
            {
                char c = _text[_pos];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                {
                    _pos++;
                }
                else
                {
                    break;
                }
            }

            string text = _text.Substring(start, _pos - start);
            if (text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0)
            {
                decimal dec;
                if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out dec)) return dec;
            }
            long integer;
            if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out integer)) return integer;
            return 0;
        }

        private void SkipWhiteSpace()
        {
            while (_pos < _text.Length && Char.IsWhiteSpace(_text[_pos])) _pos++;
        }

        private bool Peek(char expected)
        {
            SkipWhiteSpace();
            return _pos < _text.Length && _text[_pos] == expected;
        }

        private void Expect(char expected)
        {
            SkipWhiteSpace();
            if (_pos >= _text.Length || _text[_pos] != expected)
            {
                throw new FormatException("Ошибка чтения JSON: ожидался символ '" + expected + "'.");
            }
            _pos++;
        }

        private void Expect(string expected)
        {
            SkipWhiteSpace();
            if (_pos + expected.Length > _text.Length || String.Compare(_text, _pos, expected, 0, expected.Length, StringComparison.Ordinal) != 0)
            {
                throw new FormatException("Ошибка чтения JSON: ожидалось значение " + expected + ".");
            }
            _pos += expected.Length;
        }
    }
}
