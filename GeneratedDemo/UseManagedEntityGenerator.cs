using System;
using System.Text.Json;

using Persisted;


namespace GeneratedDemo
{
	[Managed(EnableAudit = true, EnableSoftDelete = true)]
	public partial class ManagedEntity
	{

		[Persisted(SetOnInsert = true)]
		private string _userIdExternal;

		/// <summary>
		/// The id used by Azure Notification Hub
		/// </summary>
		[Persisted(SetOnInsert = true)]
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
			ManagedEntity entityAuthority = new ManagedEntity { Id = authId, IdRegistration = "1"};
			ManagedEntity entityToCreate = new ManagedEntity { Id = Guid.NewGuid(), IdRegistration = "2", CreatedBy = "user1" };
			ManagedEntity entityToUpdate = new ManagedEntity { Id = Guid.NewGuid(), IdRegistration = "3", ModifiedLastBy = "user2" };
			ManagedEntity entityToDelete = new ManagedEntity { Id = Guid.NewGuid(), IdRegistration = "4", DeletedBy = "user3" };

			Console.WriteLine("\n auth entity:");
			Console.WriteLine(JsonSerializer.Serialize(entityAuthority));
			Console.WriteLine("\n auth entity after insert:");
			Console.WriteLine(JsonSerializer.Serialize(entityToCreate.MapToAuthorityInsert(entityAuthority)));
			Console.WriteLine("\n auth entity after update:");
			Console.WriteLine(JsonSerializer.Serialize(entityToUpdate.MapToAuthorityUpdate(entityAuthority)));
			Console.WriteLine("\n auth entity after delete:");
			Console.WriteLine(JsonSerializer.Serialize(entityToDelete.MapToAuthorityDelete(entityAuthority)));
		}
	}
}
