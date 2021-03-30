using System;
using System.Text.Json;

using Persisted;


namespace GeneratedDemo
{
	// The view model we'd like to augment
	[Managed(EnableAudit = true, EnableSoftDelete = true)]
	public partial class ManagedEntity
	{

		[Persisted(SetOnInsert = true)]
		private string _userIdExternal;

		/// <summary>
		/// The id used by Azure Notification Hub
		/// </summary>
		[Persisted(SetOnInsert = true, SetOnUpdate = true)]
		private string _idRegistration;

		//[Persisted(SetOnInsert = true, TypeOverride = typeof(int))]
		//private DevicePlatforms _platform;

		/// <summary>
		/// The registration id, token or URI obtained from platform-specific notification service
		/// </summary>
		[Persisted(SetOnInsert = true, SetOnUpdate = true)]
		private string _pushChannel;

	}

	public static class UseManagedEntityGenerator

	{
		public static void Run()
		{
			Guid authId = Guid.NewGuid();
			ManagedEntity entityAuthority = new ManagedEntity { Id = authId, UserIdExternal = "Shai" };
			ManagedEntity entityToCreate = new ManagedEntity { Id = Guid.NewGuid(), UserIdExternal = "John" };
			ManagedEntity entityToUpdate = new ManagedEntity { Id = Guid.NewGuid(), UserIdExternal = "Bill" };
			ManagedEntity entityToDelete = new ManagedEntity { Id = Guid.NewGuid(), UserIdExternal = "Mo" };

			Console.WriteLine("\n auth entity:");
			Console.WriteLine(JsonSerializer.Serialize(entityAuthority));
			entityAuthority = new ManagedEntity { Id = authId, UserIdExternal = "Shai" };
			Console.WriteLine("\n auth entity after insert:");
			Console.WriteLine(JsonSerializer.Serialize(entityToCreate.MapToAuthorityInsert(entityAuthority)));
			entityAuthority = new ManagedEntity { Id = authId, UserIdExternal = "Shai" };
			Console.WriteLine("\n auth entity after update:");
			Console.WriteLine(JsonSerializer.Serialize(entityToUpdate.MapToAuthorityUpdate(entityAuthority)));
			entityAuthority = new ManagedEntity { Id = authId, UserIdExternal = "Shai" };
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
