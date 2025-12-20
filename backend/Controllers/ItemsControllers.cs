using System.Buffers.Text;
using Backend.Const;
using Backend.DataAccess.DBcontexts;
using Backend.DataAccess.Entities;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;


namespace Backend.Controllers
{
    [Route("api/v1/items")]
    [ApiController]
    public class ItemsControllers : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly StoreContext _context;
        private readonly Snowflake _snowflake;
        private readonly MinioOptions _minioOptions;
        private readonly IMinioClient _minio;
        public ItemsControllers(IConfiguration config, IOptions<MinioOptions> options, StoreContext storeContext, IMinioClient minio, Snowflake snowflake)
        {

            _config = config;
            _minioOptions = options.Value;
            _context = storeContext;
            _minio = minio;
            _snowflake = snowflake;



        }

        // CURD
        [HttpPost("add")]
        public async Task<ActionResult> AddItem([FromBody] RequsetItems requsetItems)
        {

            if (requsetItems == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid request body"));

            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var item = new Items()
                {
                    ItemName = requsetItems.ItemName,
                    BasePrice = requsetItems.BasePrice,
                    StockQty = requsetItems.StockQty,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                };

                bool exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_minioOptions.MinioBucket));

                if (!exists)
                {
                    await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_minioOptions.MinioBucket));
                }

                var imageIds = new List<long>();
                foreach (var image in requsetItems.ImageList)
                {
                    if (string.IsNullOrWhiteSpace(image))
                        continue;

                    byte[] bytes;
                    try
                    {

                        bytes = ImageConverter.DecodeBase64Image(image);
                    }
                    catch
                    {
                        return BadRequest(ApiResponse<string>.Fail("Invalid base64 image"));
                    }
                    using var ms = new MemoryStream(bytes);
                    var snowflakeId = _snowflake.NextId();
                    imageIds.Add(snowflakeId);
                    await _minio.PutObjectAsync(
                        new PutObjectArgs()
                        .WithBucket(_minioOptions.MinioBucket)
                        .WithObject(snowflakeId.ToString())
                        .WithStreamData(ms)
                        .WithObjectSize(ms.Length)
                        .WithContentType("image/jpeg")
                    );
                }

                item.ImagesId = string.Join(",", imageIds);


                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(ApiResponse<string>.Ok(""));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<string>.Fail("Add failed", new[] { ex.Message })
                );
            }
        }

        [HttpPost("update/{id}")]
        public async Task<ActionResult> UpdateItem(int id, [FromBody] RequsetItems requsetItems)
        {
            if (requsetItems == null)
                return BadRequest(ApiResponse<string>.Fail("Invalid request body"));

            var item = await _context.Items.FindAsync(id);

            if (item == null)
                return NotFound(ApiResponse<string>.Fail("Item not found"));


            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Upload new Image
                var newImageIds = new List<long>();
                if (requsetItems.ImageList != null && requsetItems.ImageList.Any())
                {
                    foreach (var base64 in requsetItems.ImageList)
                    {
                        if (string.IsNullOrWhiteSpace(base64))
                            continue;

                        byte[] bytes;
                        try
                        {
                            bytes = ImageConverter.DecodeBase64Image(base64);
                        }
                        catch
                        {
                            return BadRequest(ApiResponse<string>.Fail("Invalid base64 image"));
                        }

                        var imageId = _snowflake.NextId();
                        newImageIds.Add(imageId);

                        using var ms = new MemoryStream(bytes);

                        await _minio.PutObjectAsync(
                            new PutObjectArgs()
                                .WithBucket(_minioOptions.MinioBucket)
                                .WithObject(imageId.ToString())
                                .WithStreamData(ms)
                                .WithObjectSize(ms.Length)
                                .WithContentType("image/jpeg")
                        );
                    }
                }

                // 2. Delete old images AFTER new upload succeeds
                var oldImageIds = item.ImagesId?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    ?? Array.Empty<string>();

                foreach (var imageId in oldImageIds)
                {
                    await _minio.RemoveObjectAsync(
                        new RemoveObjectArgs()
                            .WithBucket(_minioOptions.MinioBucket)
                            .WithObject(imageId)
                    );
                }

                item.ImagesId = string.Join(",", newImageIds);
                item.ItemName = requsetItems.ItemName;
                item.BasePrice = requsetItems.BasePrice;
                item.StockQty = requsetItems.StockQty;
                item.UpdateAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<string>.Ok(""));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<string>.Fail("Update failed", new[] { ex.Message })
                );
            }
        }

        [HttpGet("readitem")]
        public async Task<ActionResult> ReadAllItems([FromQuery] int limit = 50, [FromQuery] int page = 1)
        {
            if (limit <= 0 || page <= 0)
                return BadRequest(ApiResponse<string>.Fail("Limit and page must be greater than zero"));
            try
            {
                var list = await _context.ReadItems
                .FromSqlInterpolated($"EXEC [dbo].[get_all_items] @limit={limit}, @page={page}")
                .AsNoTracking()
                .ToListAsync();

                if (!list.Any())
                {
                    return Ok(ApiResponse<ReadItemsResponse>.Ok(new ReadItemsResponse
                    {
                        Items = Array.Empty<ItemDto>(),
                        TotalCount = 0,
                        TotalPage = 0
                    }));
                }
                var response = new ReadItemsResponse
                {
                    Items = list.Select(item => new ItemDto
                    {
                        ItemName = item.ItemName ?? "",
                        ImagesId = item.ImagesId?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            ?? Array.Empty<string>(),
                        BasePrice = item.BasePrice,
                        StockQty = item.StockQty,
                        CreateAt = item.CreateAt,
                        UpdateAt = item.UpdateAt
                    }),
                    TotalCount = list.First().TotalCount,
                    TotalPage = list.First().TotalPages
                };
                return Ok(ApiResponse<ReadItemsResponse>.Ok(response));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<string>.Fail("Read items failed", new[] { ex.Message })
                );
            }
        }

        [HttpGet("readitem/{id}")]
        public async Task<ActionResult> ReadItems(int id)
        {
            try
            {
                var item = await _context.Items.FindAsync(id);
                if (item == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Item not found"));
                }
                var response = new ReadItemsResponse
                {
                    Items = [
                      new ItemDto(){
                          ItemName= item.ItemName?? "",
                          ImagesId= item.ImagesId?.Split(",", StringSplitOptions.RemoveEmptyEntries)?? Array.Empty<string>(),
                          BasePrice= item.BasePrice,
                          StockQty= item.StockQty,
                          CreateAt= item.CreateAt,
                          UpdateAt= item.UpdateAt,
                      },
                    ],
                    TotalCount = 1,
                    TotalPage = 1,
                };
                return Ok(ApiResponse<ReadItemsResponse>.Ok(response));

            }
            catch (Exception ex)
            {
                return StatusCode(
                            StatusCodes.Status500InternalServerError,
                            ApiResponse<string>.Fail("Read item failed", new[] { ex.Message })
                        );
            }
        }

        [HttpGet("getImage")]
        public async Task<ActionResult> GetImage([FromQuery] string imageId)
        {
            if (string.IsNullOrWhiteSpace(imageId))
                return BadRequest(ApiResponse<string>.Fail("imageId is required"));
            try
            {

                // 1. Get object metadata (content-type, size)
                var stat = await _minio.StatObjectAsync(
                    new StatObjectArgs()
                        .WithBucket(_minioOptions.MinioBucket)
                        .WithObject(imageId)
                );

                using var ms = new MemoryStream();

                await _minio.GetObjectAsync(
                    new GetObjectArgs()
                    .WithBucket(_minioOptions.MinioBucket)
                    .WithObject(imageId)
                    .WithCallbackStream(stream => stream.CopyTo(ms))
                );
                ms.Position = 0; // important
                var base64 = Convert.ToBase64String(ms.ToArray());

                return Ok(ApiResponse<string>.Ok(
                    $"data:{stat.ContentType};base64,{base64}"
                ));

            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return NotFound(ApiResponse<string>.Fail("Image not found"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                   StatusCodes.Status500InternalServerError,
                   ApiResponse<string>.Fail("Image is not found", new[] { ex.Message })
               );
            }
        }

    }
}
