CREATE TABLE [dbo].[items] (
    [id]         INT            IDENTITY (1, 1) NOT NULL,
    [images_id]  NVARCHAR (MAX) NULL,
    [Item_name]  NVARCHAR (100) NULL,
    [base-price] MONEY          NULL,
    [stock_qty]  INT            NULL,
    [create_at]  DATETIME       NULL,
    [update_at]  DATETIME       NULL,
    CONSTRAINT [PK_items] PRIMARY KEY CLUSTERED ([id] ASC)
);

