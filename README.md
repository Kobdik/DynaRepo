## DynaLib

Основной целью проекта является разработка инструментария на базе ADO.NET, для вызова хранимых процедур БД, передачи возвращаемых данных из `IDataReader` сразу в выходной поток без необходимости создания объектов модели для последующей их сериализации. Такой подход применим при реализации трёх-звенной архитектуры приложения или WEB API интерфейса, так как на стороне сервера нет необходимости во взаимодействии с объектами-сущностями, достаточно лишь последовательно прочитать из реализации `IDataReader` данные свойств и записать их в поток.

Высокая скорость работы `SqlDataReader` обусловлена применением `FAST_FORWARD` курсора на стороне БД, однако, непосредственно использовать его неудобно, получается слишком много ручного кодирования. Поэтому, для доступа к данным прибегают к помощи `EF` (Entity Framework), который внутренне использует `SqlDataReader`, однако, скорость загрузки данных падает от 3-х до более чем в 10 раз. Забегая вперёд скажу, что в моих тестовых замерах производительности относительная скорость `EF` ниже в 3.7 раза при больших выборках, когда запрос возвращает более 70 000 записей, а на малых выборках, менее 1 000 записей, относительная скорость падает в 17 раз!

Для решения проблем с производительностью `EF`, а точнее, для того чтобы не использовать его вовсе, была разработана библиотека `DynaLib`, главная роль в которой принадлежит классу `DynaObject`, который умеет читать параметры из входного потока, вызывать хранимые процедуры на стороне БД, непосредственно работать с `IDataReader`, записывая данные из него в выходной поток в `binary`, `json` или `xml` форматах.

Скачайте и скомпилируйте библиотеку из проекта [DynaLib](https://github.com/Kobdik/DynaRepo/tree/master/DynaLib). 

# Dictionary First

Для ознакомления скачайте БД (SQL Server 2012, LocalDB) из каталога Loc_Db. Организация словарей метаданных в форме таблиц описаний позволяет эффективно управлять запросами на основе хранимых процедур, унифицирует и упрощает обработку данных, способствует применению декларативного стиля программирования. Подробнее тут: [Dictionary First](https://github.com/Kobdik/DynaRepo/blob/master/docs/Dictionary.md)

# DynaObject

Использование объектов `DynaObject` на примере запроса Invoice. В своем проекте добавьте ссылку на библиотеку `DynaLib`, затем добавьте пространства имен.
```csharp
using Kobdik.Common;
using Kobdik.DataModule;
```
В конфигурационном файле приложения *App.config* добавьте следующие строки, указав физический путь к файлам БД.
```
  <appSettings>
    <add key="Conn" value="Data Source=(LocalDb)\v11.0;Integrated Security=True;AttachDbFileName=D:\work\DynaRepo\Loc_Db\test.mdf"/>
  </appSettings>
```
Объявите статическую переменную dataMod:
```csharp
 static DataMod dataMod = DataMod.Current();
```
Теперь можно исполнить select-запрос:
```csharp
 IDynaObject dynaObject = dataMod.GetDynaObject("Invoice");
 //имитация входного потока с json данными
 using (FileStream rfs = new FileStream("Invoice_Params.json", FileMode.Open))
 {
  //считываем параметры из входного потока
  dynaObject.ReadPropStream(rfs, "sel");
  //задать параметры select-запроса можно в коде 
  //dynaObject.ParmDict["Dt_Fst"].Value = "2017.01.01";
  //dynaObject.ParmDict["Dt_Lst"].Value = "2017.07.31";
 }
 //исполним select-запрос, 
 //результат пишем в файловый поток,
 //имитируя выгрузку в выходной json-поток
 using (FileStream fs = new FileStream("Invoice.json", FileMode.Create))
 {
  dynaObject.SelectToStream(fs);
 }
```
Файл параметров и результирующая выборка находятся в каталоге [QueryApp](https://github.com/Kobdik/DynaRepo/tree/master/QueryApp)

Подробнее о [DynaObject](https://github.com/Kobdik/DynaRepo/blob/master/docs/DynaObject.md), о запросах `LINQ to Objects` на основе обобщенного класса [DynaQuery<T>](https://github.com/Kobdik/DynaRepo/blob/master/docs/DynaQuery.md), замеры производительности в сравнении с `LINQ to EF` [Challenge](https://github.com/Kobdik/DynaRepo/blob/master/docs/Challenge.md)
