CREATE PROCEDURE [dbo].[InsertUser]
    @ID BIGINT,
    @Name [nchar](10)
AS
BEGIN TRY
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRAN

    INSERT INTO [dbo].[User]
    (
        [ID],
        [Name]
    )
    VALUES
    (
        @ID,
        @Name
    )

    COMMIT TRAN
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRAN

    THROW;
END CATCH