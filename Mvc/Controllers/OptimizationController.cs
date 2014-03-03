using SitefinityWebApp.Custom.AlbumOptimization;
using System;
using System.Web.Http;
using Telerik.Sitefinity.Scheduling;

namespace SitefinityWebApp.Mvc.Controllers
{
    public class OptimizationController : ApiController
    {
        [Authorize]
        [HttpPost]
        public Guid Post(Guid id)
        {
            return StartOptimizeAlbumItemsTask(id);
        }

        private Guid StartOptimizeAlbumItemsTask(Guid albumId)
        {
            SchedulingManager manager = SchedulingManager.GetManager();

            // This Guid is passed back to Sitefinity so that it can monitor the task progress
            Guid guid = Guid.NewGuid();

            AlbumOptimizationTask albumOptimizationTask = new AlbumOptimizationTask()
            {
                Id = guid,
                AlbumId = albumId
            };

            manager.AddTask(albumOptimizationTask);

            manager.SaveChanges();

            return guid;
        }
    }
}