-- 在目标SQL Server库中执行

ALTER TABLE User_User ADD SrcId BIGINT NOT NULL DEFAULT(0)
ALTER TABLE Finance_Account ADD SrcId BIGINT NOT NULL DEFAULT(0)
ALTER TABLE dbo.User_UserTypeIndex ADD SrcId BIGINT NOT NULL DEFAULT(0)
ALTER TABLE dbo.User_PlacementRelation ADD SrcId BIGINT NOT NULL DEFAULT(0)

ALTER TABLE dbo.FenRun_LevelTouchValue ADD SrcId BIGINT NOT NULL DEFAULT(0)


-- 更新UserTypeIndex的Key值
UPDATE dbo.User_UserTypeIndex SET [Key]='ZKCloud.App.Core.UserType.Modules.ServiceCenter.ServiceCenterUserType' WHERE UserTypeId='71BE65E6-3A64-414D-972E-1A3D4A365666'
UPDATE dbo.User_UserTypeIndex SET [Key]='ZKCloud.App.Core.User.Domain.CallBacks.UserGradeConfig' WHERE SrcId>0 AND UserTypeId<>'71BE65E6-3A64-414D-972E-1A3D4A365666'
