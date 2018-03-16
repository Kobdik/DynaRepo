# DynaLib

Основной целью проекта является разработка инструментария на базе ADO.NET, для вызова хранимых процедур БД, передачи возвращаемых данных из `IDataReader` сразу в выходной поток без необходимости создания объектов модели для последующей их сериализации. Такой подход применим при реализации трёх-звенной архитектуры приложения или WEB API интерфейса, так как на стороне сервера нет необходимости во взаимодействии с объектами-сущностями, достаточно лишь последовательно прочитать из реализации `IDataReader` данные свойств и записать их в поток.

Высокая скорость работы `SqlDataReader` обусловлена применением `FAST_FORWARD` курсора на стороне БД, однако, непосредственно использовать его неудобно, получается слишком много ручного кодирования. Поэтому, для доступа к данным прибегают к помощи `EF` (Entity Framework), который внутренне использует `SqlDataReader`, однако, скорость загрузки данных падает от 3-х до более чем в 10 раз. Забегая вперёд скажу, что в моих тестовых замерах производительности относительная скорость загрузки данных `EF` ниже в 2.5 раза, а если еще и выгружать данные в json, то - в 6 раз!

*Классы сущностей модели
*Контроллеры и методы действий
*Отображение методов HTTP на действия
*Привязка моделей
*Исполнение SQL операторов
*ООП и функциональный стиль
*Сериализация данных в поток
*LINQ to Entities
*Снижение скорости получения данных


Для решения проблем с производительностью `EF`, а точнее, для того чтобы не использовать его вовсе, была разработана библиотека `DynaLib`, главная роль в которой принадлежит классу `DynaObject`, который умеет читать параметры из входного потока, вызывать хранимые процедуры на стороне БД, непосредственно работать с `IDataReader`, записывая данные из него в выходной поток в `binary`, `json` или `xml` форматах.

Скачайте и скомпилируйте библиотеку из проекта [DynaLib](https://github.com/Kobdik/DynaRepo/tree/master/DynaLib). 

## Dictionary First

Для ознакомления скачайте БД (SQL Server 2012, LocalDB) из каталога Loc_Db. Организация словарей метаданных в форме таблиц описаний позволяет эффективно управлять запросами на основе хранимых процедур, унифицирует и упрощает обработку данных, способствует применению декларативного стиля программирования. Подробнее тут: [Dictionary First](https://github.com/Kobdik/DynaRepo/blob/master/docs/Dictionary.md)

## Использование DynaObject

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
В моем действующем WEB API приложении параметры приходят в теле post-запроса, что позволяет избежать привязки моделей. Более того, не нужно создавать отдельный контроллер под каждый тип запроса. Достаточно одного контроллера c 4-5 точками входа, обрабатывающего запросы в стиле RPC. Вот пример фрагмента из *WebApiConfig.cs*
```
config.Routes.MapHttpRoute(
 name: "DynaInit",
 routeTemplate: "api/Dyna/init",
 //no query parameters
 defaults: new { controller = "Dyna", action = "Init" }
);

config.Routes.MapHttpRoute(
 name: "DynaSelect",
 routeTemplate: "api/Dyna/sel/{qry}",
 defaults: new { controller = "Dyna", action = "SelectJson" }
);

config.Routes.MapHttpRoute(
 name: "DynaDetail",
 routeTemplate: "api/Dyna/get/{qry}/{idn}", 
 defaults: new { controller = "Dyna", action = "DetailJson" }
);

config.Routes.MapHttpRoute(
 name: "DynaAction",
 routeTemplate: "api/Dyna/{cmd}/{qry}",
 defaults: new { controller = "Dyna", action = "ActionJson" }
);
```

Файл параметров и результирующая выборка находятся в каталоге [QueryApp](https://github.com/Kobdik/DynaRepo/tree/master/QueryApp)

Подробнее об устройстве [DynaObject](https://github.com/Kobdik/DynaRepo/blob/master/docs/DynaObject.md).

О запросах `LINQ to Objects` на основе обобщенного класса [DynaQuery](https://github.com/Kobdik/DynaRepo/blob/master/docs/DynaQuery.md).

Замеры производительности в сравнении с `LINQ to EF` [Challenge](https://github.com/Kobdik/DynaRepo/blob/master/docs/Challenge.md).
