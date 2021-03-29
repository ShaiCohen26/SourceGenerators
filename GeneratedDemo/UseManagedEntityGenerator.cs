using System;
using System.Text.Json;

using Persisted;


namespace GeneratedDemo
{
	// The view model we'd like to augment
	public partial class ManagedEntity
	{
		[Persisted(PersistenceType.Direct, SetOnInsert = true, SetOnDelete = true)]
		private Guid _id;

		[Persisted(PersistenceType.Direct, SetOnInsert = true, SetOnUpdate = true)]
		private string _name;
	}

	public static class UseManagedEntityGenerator

	{
		public static void Run()
		{
			Guid authId = Guid.NewGuid();
			ManagedEntity entityAuthority = new ManagedEntity { Id = authId, Name = "Shai" };
			ManagedEntity entityToCreate = new ManagedEntity { Id = Guid.NewGuid(), Name = "John" };
			ManagedEntity entityToUpdate = new ManagedEntity { Id = Guid.NewGuid(), Name = "Bill" };
			ManagedEntity entityToDelete = new ManagedEntity { Id = Guid.NewGuid(), Name = "Mo" };

			Console.WriteLine("\n auth entity:");
			Console.WriteLine(JsonSerializer.Serialize(entityAuthority));
			entityAuthority = new ManagedEntity { Id = authId, Name = "Shai" };
			Console.WriteLine("\n auth entity after insert:");
			Console.WriteLine(JsonSerializer.Serialize(entityToCreate.MapToAuthorityInsert(entityAuthority)));
			entityAuthority = new ManagedEntity { Id = authId, Name = "Shai" };
			Console.WriteLine("\n auth entity after update:");
			Console.WriteLine(JsonSerializer.Serialize(entityToUpdate.MapToAuthorityUpdate(entityAuthority)));
			entityAuthority = new ManagedEntity { Id = authId, Name = "Shai" };
			Console.WriteLine("\n auth entity after delete:");
			Console.WriteLine(JsonSerializer.Serialize(entityToDelete.MapToAuthorityDelete(entityAuthority)));




			// we didn't explicitly create the 'Text' property, it was generated for us 
			//string text = vm.Text;
			//Console.WriteLine($"Text = {text}");

			//// Properties can have differnt names generated based on the PropertyName argument of the attribute
			//int count = vm.Count;
			//Console.WriteLine($"Count = {count}");

			//// the viewmodel will automatically implement INotifyPropertyChanged
			//vm.PropertyChanged += (o, e) => Console.WriteLine($"Property {e.PropertyName} was changed");
			//vm.Text = "abc";
			//vm.Count = 123;

			// Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
			// You'll see the matching generated properties visibile in IntelliSense in realtime
		}
	}
}
