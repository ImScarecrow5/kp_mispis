Проект для Visual Studio 2022

Как открыть:
1. Распаковать архив в отдельную папку.
2. Открыть файл FdmPrinterProductionSystem.sln.
3. В Visual Studio выбрать Build -> Rebuild Solution.
4. Запустить проект кнопкой Start.

Технологии:
- C#
- Windows Forms
- .NET Framework 4.8
- база данных SQL Server LocalDB

База создается автоматически при первом запуске:
%APPDATA%\FdmPrinterProductionSystem\fdm_production.mdf

Если при запуске появится ошибка LocalDB, нужно установить компонент SQL Server Express LocalDB через Visual Studio Installer:
Individual components -> SQL Server Express LocalDB.
