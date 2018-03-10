## DynaObject

Класс `DynaObject` служит адаптером к таблице на стороне БД, вызывая хранимые процедуры `select, detail, insert, update`. Для создания объектов `DynaObject` используется фабричный метод GetDynaObject(string queryName) singleton экземпляра DataMod, который при инициализации загружает необходимые мета-данные из БД, чтобы потом снабдить ими создаваемые объекты, а также устанавливаетет им `binary`, `json` или `xml` форматтеры для записи и чтения из потоков.

Посмотрите исходные коды в модуле [DynaLib/Dynamics](https://github.com/Kobdik/DynaRepo/blob/master/DynaLib/Dynamics.cs).

Рассмотрим наиболее интересные члены класса. Словарь *Dictionary<String, IDynaProp> ParmDict* - хранит информацию о параметрах  select-запроса, а *Dictionary<String, IDynaProp> PropDict* - о колонках полей запросов. 
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
Первые пять членов и свойство *Value* интерфейса позволяют точно указать параметры хранимых процедур при создании `IDbCommand`. Свойство *Ordinal* сразу после исполнения select-запроса получает значение индекса колонки в `IDataReader`, что позволяет эффективно читать значения поля из результатов запроса. Метод *WriteProp(IDataRecord record, IPropWriter writer)* читает из `IDataReader : IDataRecord` и сразу пишет в выходной поток.

На сегодня реализованы следующие *sealed* классы, реализующие `IDynaProp` - это StringProp, ByteProp, Int16Prop, Int32Prop, DateProp, DoubleProp. Каждый из них переопределяет виртуальные методы чтения и записи.

Операции записи простых свойств в поток определены в интерфейсе `IPropWriter`.
```csharp
public interface IPropWriter
{
 void WriteProp(String propName, String value);
 void WriteProp(String propName, Byte value);
 void WriteProp(String propName, Int16 value);
 void WriteProp(String propName, Int32 value);
 void WriteProp(String propName, DateTime value);
 void WriteProp(String propName, Double value);
}
```
Реализация записи в поток отделена от `DynaObject` через объявление свойства-интерфейса *StreamWriter : IStreamWriter*.
```csharp
public interface IStreamWriter : IPropWriter
{
 byte GetStreamType();
 void Open(Stream stream);
 void PushArr();
 void PushObj();
 void PushArrProp(string propName);
 void PushObjProp(string propName);
 void Pop();
 void Close();
 string Result { get; }
}
```
Методы интерфейса позволяют единообразно, в единой логической структуре, формировать ответ клиенту в json, xml или binary форматах. Например, при вызове *SelectToStream(Stream stream)* 
1. Инициализируем *StreamWriter* на основе выходного потока
2. Погружаемся в контекст неименованного объекта
3. Погружаемся в контекст именнованного в "selected" свойства-массива
4. Запоминаем текущее время и исполняем запрос
5. Получив `IDataReader`, в цикле читаем записи
6. Погружаемся в неименованный контекст объекта каждой записи
7. *WriteRecord* вызывает на задействованных `IDynaProp` метод *WriteProp*
8. Выходим из контекста неименованного объекта
9. Закрываем `IDataReader`
10. Сравнивая текущее время и запомненное в начале вычисляем время работы
11. Выходим из контекста именованного массива
12. Пишем в именнованное свойство "message" сообщение от *Query*
13. Пишем в именнованное свойство "sel_time" время записи
14. Пишем в именнованное свойство "time_ms" длительность записи
15. Выходим из контекста неименованного объекта
16. Закрываем *StreamWriter*
```
 StreamWriter.Open(stream);
 StreamWriter.PushObj();
 StreamWriter.PushArrProp("selected");
 DateTime fst = DateTime.Now;
 selReader = Select();
 if (selReader != null)
 {
  while (selReader.Read())
  {
   StreamWriter.PushObj();
   WriteRecord(selReader, ReadList, StreamWriter);
   StreamWriter.Pop();
  }
  selReader.Close();
 };
 DateTime lst = DateTime.Now;
 TimeSpan ts = lst - fst;
 StreamWriter.Pop();
 StreamWriter.WriteProp("message", Query.Result);
 StreamWriter.WriteProp("sel_time", lst.ToShortTimeString());
 StreamWriter.WriteProp("time_ms", ts.Milliseconds);
 StreamWriter.Pop();
 StreamWriter?.Close();
```
На выходе получаем json-файл следующего вида, где {row} - сокращение для записи строк:
```
{"selected":[{row},{row},{row}],"message":"Ok","sel_time":"11:19","time_ms":15}
```


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
