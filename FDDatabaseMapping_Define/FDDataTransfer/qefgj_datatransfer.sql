# mysql中先执行该脚本
use qefgj;

DROP PROCEDURE IF EXISTS `getParentAccount`;
DELIMITER //
CREATE PROCEDURE `getParentAccount`(accountId INT)
BEGIN
	DECLARE sTemp VARCHAR(4000);
	DECLARE parentId INT;
	DECLARE ecount INT;
	#declare rowNo int default 0;
	CREATE TEMPORARY TABLE tempuser
	(
		id INT PRIMARY KEY AUTO_INCREMENT,
		userId INT,
		username VARCHAR(60)
	)ENGINE=MyISAM DEFAULT CHARSET=utf8;
	
	SET parentId=0;
	SELECT u2.UE_ID,u2.UE_account INTO parentId,sTemp FROM qefgj_user AS u
	INNER JOIN qefgj_user AS u2 ON u.zcr=u2.UE_account
	 WHERE u.UE_ID=accountId AND u2.UE_ID<>accountId;
	
	out_label: BEGIN
	WHILE TRUE DO
		INSERT INTO tempuser(userId,username) VALUES (parentId,sTemp);
		
		SELECT u2.UE_ID,u2.UE_account INTO parentId,sTemp FROM qefgj_user AS u
		INNER JOIN qefgj_user AS u2 ON u.zcr=u2.UE_account
		WHERE u.UE_ID=parentId AND u2.UE_ID<>accountId;
		
		SELECT COUNT(*) INTO ecount FROM tempuser WHERE userId=parentId;
		IF (ecount > 0) THEN
			LEAVE out_label;
		END IF;
		#select 1 into userExists from tempuser where userId=userId limit 1;
		#set rowNo=rowNo+1;
		
	END WHILE;
	END out_label;
	
	SELECT * FROM tempuser ORDER BY id DESC;
	DROP TABLE tempuser;
END;


DROP PROCEDURE IF EXISTS `getParentPlace`;
DELIMITER //
CREATE PROCEDURE `getParentPlace`(accountId INT)
BEGIN
	DECLARE sTemp VARCHAR(4000);
	DECLARE parentId INT;
	DECLARE parePosition INT;
	DECLARE ecount INT;
	#declare rowNo int default 0;
	CREATE TEMPORARY TABLE tempuser
	(
		id INT PRIMARY KEY AUTO_INCREMENT,
		userId INT,
		location INT,
		username VARCHAR(60)
	)ENGINE=MyISAM DEFAULT CHARSET=utf8;
	
	SET parentId=0;
	SELECT u2.UE_ID,u2.UE_account,u2.tree_position INTO parentId,sTemp,parePosition FROM qefgj_user AS u
	INNER JOIN qefgj_user AS u2 ON u.UE_accName=u2.UE_account
	 WHERE u.UE_ID=accountId;
	
	out_label: BEGIN
	WHILE TRUE DO
		
		INSERT INTO tempuser(userId,username,location) VALUES (parentId,sTemp,parePosition);
		
		SELECT u2.UE_ID,u2.UE_account,u2.tree_position INTO parentId,sTemp,parePosition FROM qefgj_user AS u
		INNER JOIN qefgj_user AS u2 ON u.UE_accName=u2.UE_account
		WHERE u.UE_ID=parentId;
		
		SELECT COUNT(*) INTO ecount FROM tempuser WHERE userId=parentId;
		IF (ecount > 0) THEN
			LEAVE out_label;
		END IF;
		
	END WHILE;
	END out_label;
	
	SELECT * FROM tempuser ORDER BY id DESC;
	DROP TABLE tempuser;
END