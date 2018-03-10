## DynaObject

Класс `DynaObject` служит адаптером к таблице на стороне БД, вызывая хранимые процедуры `select, detail, insert, update`. Для создания объектов `DynaObject` используется фабричный метод `IDynaObject DataMod.GetDynaObject(string queryName)`. Singleton экземпляр DataMod при инициализации загружает всю необходимую мета-информацию из БД, используемую при создании объектов, а также снабжает их `binary`, `json` или `xml` форматтерами для записи и чтения из потоков.

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
