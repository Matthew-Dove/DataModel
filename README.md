DataModel
=========

A Model to SQL library that helps load and save .NET classes to SQL SERVER via stored procedures.


This project is a class library built with the .NET 4 framework.
If you want to use this, add this project to another existing project, and have that project reference this one.
Otherwise you can open this project, build it, and use the DataLayer.dll and FastMember.dll in your existing project as references.


Use stored procedures to load or save models.

Load:

MyMessage myMessage = SqlLayer.LoadModel<MyMessage>("usp_LoadModel", new SqlParameter("@Id", 99));

Save:

var saveTest = new SaveTest();
saveTest.Id = 1;
saveTest .Message = "Hello world";
SqlLayer.SaveModel<SaveTest>("usp_SaveModel", saveTest);


Models have Attributes for output parameters, return values etc:

[SqlAlias]
[SqlIgnorge]
[SqlOutput]
[SqlReturn]

These go on the corresponding model properties.


There is also a connection string handler, use as many connections as you want.

SqlLayer.ConnectionString.Upsert("MyKey", connection);


If you have something more complicated then a plain model, there is a helper class to read Sql.

var sqlProvider = new SqlProvider(connection)

This has a range of extetions to get data, such as:

var dr = sqlProvider.ExecuteReader("usp_Select_Users");
string name = dr.Get<string>("Username");
