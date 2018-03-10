## DynaObject

Класс `DynaObject` служит адаптером к таблице на стороне БД, вызывая хранимые процедуры `select, detail, insert, update`. Для создания объектов `DynaObject` используется фабричный метод `IDynaObject DataMod.GetDynaObject(string queryName)`. Singleton экземпляр DataMod при инициализации загружает всю необходимую мета-информацию из БД, используемую при создании объектов, а также снабжает их `binary`, `json` или `xml` форматтерами для записи и чтения из потоков.

Посмотрите исходный код класса в модуле [DynaLib/Dynamics](https://github.com/Kobdik/DynaRepo/blob/master/DynaLib/Dynamics.cs)

Рассмотрим наиболее интересные члены класса. Словарь `ParmDict` - хранит информацию о параметрах  select-запроса, а `PropDict` - о полях запросов. Метаданные полей хранятся в объектах, реализующих интерфейс `IDynaProp`
```csharp
public interface IDynaProp
{
 string GetName();
 DbType GetDbType();
 Type GetPropType();
 int GetSize();
 byte GetFlags();
 int Ordinal { get; set; }
 //объявлены виртуальными
 //в базовом классе DynaProp
 void ReadProp(IDataRecord record);
 void WriteProp(IDataRecord record, IPropWriter writer);
 void WriteProp(IPropWriter writer);
 Object Value { get; set; }
}
```
Первые пять членов и свойство Value интерфейса позволяют точно указать параметры хранимых процедур, верно записать данные в выходной поток. Свойство Ordinal сразу после исполнения select-запроса получает значение индекса колонки в `IDataReader`, что позволяет эффективно читать значения поля из результатов запроса. Метод `WriteProp(IDataRecord record, IPropWriter writer)` читает из `IDataReader : IDataRecord` и сразу пишет в выходной поток.

На сегодня реализованы следующие *sealed* классы, реализующие `IDynaProp` - это StringProp, ByteProp, Int16Prop, Int32Prop, DateProp, DoubleProp. Каждый из них переопределяет виртуальные методы чтения и записи.



На стороне контроллера объекты `DynaObject` доступны через реализуемый ими интерфейс `IDynaObject`: 
```csharp
//Через интерфейс удобнее и 
//безопаснее работать с объектом
public interface IDynaObject : IDisposable
{
 //описания параметров select-запроса
 Dictionary<String, IDynaProp> ParmDict { get; }
 //описания колонок запросов
 Dictionary<String, IDynaProp> PropDict { get; }
 //читает из потока значения select-параметров 
 //или значения колонок при insert/update
 void ReadPropStream(Stream stream, string cmd);
 //пишут результаты запросов в выходной поток
 void SelectToStream(Stream stream);
 void DetailToStream(Stream stream, int idn);
 void ActionToStream(Stream stream, string cmd);
 //Служит для целей отладки, возвращает
 //имена и значения колонок PropDict
 string GetInfo();
}
```
