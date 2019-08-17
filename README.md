# standard-repository

this project is a simple alternative for entity framework...

with StandardRepository;

you get record revisions.

    Task<List<EntityRevision<T>>> SelectRevisions(long id);
    Task<bool> RestoreRevision(long currentUserId, long id, int revision);

and also soft delete logic.

    Task<bool> Delete(long currentUserId, long id);
    Task<bool> UndoDelete(long currentUserId, long id);
    Task<bool> HardDelete(long currentUserId, long id);
    
## You can install this package via NuGet
    Install-Package StandardRepository.PostgreSQL
    
## Example Project

please check the "translation" project for an example usage.

https://github.com/anatolia/translation/blob/master/Source/Translation.Client.Web/Helpers/DbGeneratorHelper.cs#L47
https://github.com/anatolia/translation/blob/master/Source/Translation.Data/Repositories/OrganizationRepository.cs#L13
https://github.com/anatolia/translation/blob/master/Source/Translation.Data/UnitOfWorks/LogOnUnitOfWork.cs#L28
    

