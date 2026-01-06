using MuhasibPro.Domain.Entities;

namespace MuhasibPro.Business.EntityModel
{
    // ModelFactory'yi güncelleyelim
    public class ModelFactory
    {
        public static TModel ReadModel<TModel, TEntity>(
            TEntity entity,
            bool includeAllFields,
            Action<TModel, TEntity, bool> customMapping = null)
            where TModel : BaseModel, new()
            where TEntity : BaseEntity
        {
            var model = new TModel();

            // Base alanları map et
            model.Id = entity.Id;
            model.KaydedenId = entity.KaydedenId;
            model.AktifMi = entity.AktifMi;
            model.KayitTarihi = entity.KayitTarihi;
            model.GuncelleyenId = entity.GuncelleyenId;
            model.AktifMi = entity.AktifMi;
            model.GuncellemeTarihi = entity.GuncellemeTarihi;
            // Özel mapping varsa uygula
            customMapping?.Invoke(model, entity, includeAllFields);

            return model;
        }

        public async static Task<TModel> CreateModelAsync<TModel, TEntity>(
            TEntity entity,
            Func<TModel, TEntity, bool, Task> asyncCustomMapping = null,
            bool includeAllFields = false)  // Opsiyonel parametre en sonda
       where TModel : BaseModel, new() where TEntity : BaseEntity
        {
            var model = new TModel();

            // Base alanları map et
            model.Id = entity.Id;
            model.KaydedenId = entity.KaydedenId;
            model.AktifMi = entity.AktifMi;
            model.KayitTarihi = entity.KayitTarihi;
            model.GuncelleyenId = entity.GuncelleyenId;
            model.GuncellemeTarihi = entity.GuncellemeTarihi;

            // Özel mapping varsa uygula
            if(asyncCustomMapping != null)
            {
                await asyncCustomMapping(model, entity, includeAllFields);
            }

            return model;
        }


        public async static Task<TModel> CreateModelAsync<TModel, TEntity>(
            TEntity entity,
            bool includeAllFields,
            Func<TModel, TEntity, bool, Task> asyncCustomMapping = null)
            where TModel : BaseModel, new()
            where TEntity : BaseEntity
        {
            var model = new TModel();

            // Base alanları map et
            model.Id = entity.Id;
            model.KaydedenId = entity.KaydedenId;
            model.GuncelleyenId = entity.GuncelleyenId;
            model.AktifMi = entity.AktifMi;  // SADECE BİR KERE
            model.KayitTarihi = entity.KayitTarihi;
            model.GuncellemeTarihi = entity.GuncellemeTarihi;

            // Özel mapping varsa uygula
            if(asyncCustomMapping != null)
            {
                await asyncCustomMapping(model, entity, includeAllFields);
            }

            return model;
        }

        public static TEntity UpdateEntity<TEntity, TModel>(TModel model, Action<TEntity, TModel> customMapping = null)
            where TEntity : BaseEntity, new()
            where TModel : BaseModel
        {
            var entity = new TEntity();

            // Base alanları map et

            entity.GuncelleyenId = model.GuncelleyenId;
            entity.AktifMi = model.AktifMi;
            entity.GuncellemeTarihi = model.GuncellemeTarihi;

            // Özel mapping varsa uygula
            customMapping?.Invoke(entity, model);

            return entity;
        }
    }
}
