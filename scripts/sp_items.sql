SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Create the stored procedure in the specified schema
ALTER PROCEDURE [dbo].[get_all_items]
    @limit int = 50,
    @page int = 1 
-- add more stored procedure parameters here
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @offset INT = (@page -1) * @limit;

    ;WITH ItemsCTE AS(
       SELECT
            *,
           CAST( COUNT(*) OVER() as int) AS TotalCount
        FROM dbo.items
    )

    -- body of the stored procedure
    SELECT
        *,
       CAST( CEILING(TotalCount * 1.0 / @limit)  as int) AS TotalPages
    FROM ItemsCTE
    ORDER BY id
    OFFSET @offset ROWS
    FETCH NEXT @limit ROWS ONLY;
END
GO

EXEC [dbo].[get_all_items] 