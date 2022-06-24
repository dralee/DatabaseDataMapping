# DatabaseDataMapping
Migration the data from other databases(Sql Server or MySQL) by the configuration relations.
---
Edit the config by json(which json file in the "config" folder):
---------------------------------------------------------------
### The details:
* **ConnStringFrom**:where the data from.
* **ConnStringTo**:where the data to.
* **DBContextTypeFrom**:the from data server provider how to connect,the database ado.net access by the database type provider(can defined by oneself),but the namespace started with "FDDataTransfer".
* **DBContextTypeTo**:the from data server provider how to connect,the database ado.net access by the database type provider(can defined by oneself),but the namespace started with "FDDataTransfer".
##### the default implemented like this:
 1. **DBContextTypeFrom**: "FDDataTransfer.SqlServer.Repositories.MySqlRepositoryContext"
 2. **DBContextTypeTo**: "FDDataTransfer.SqlServer.Repositories.SqlServerRepositoryContext"

* **QueueMaxCount**:the max quantity of the executing queue.
* **Tables**:which table info
* **TableFrom**:the table in the from database.
* **TableTo**:the table in the target database.
* **KeyFrom**:the key of the from table.
* **KeyTo**:the key of the target table.
* **PerExecuteCount**:the quantity of the per read from the database.
* **MessageType**: message type,just for the business solve(default:0,and other values by self defined)
* **Columns**:the columns mapped by target table and source table.
* **ExtendQueryColumns**:defined by the self define business to for quey from the source table by once.
* **ColumnDefaultValues**:the default value for the columns which not exists in the source table and the target table needed.

#