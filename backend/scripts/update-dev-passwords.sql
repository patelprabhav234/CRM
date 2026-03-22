-- Run once if users were created with old passwords (Admin123! / Tech123!).
-- BCrypt for: ShahFire#MaX-2025 (admin) and FieldTech#MaX-2025 (tech)

UPDATE "Users"
SET "PasswordHash" = '$2a$11$o/xQS6a2gJ/CS0wqNWs/9Ott7xkQZVqdz/O81ryFROBizWsBporQa'
WHERE "Email" = 'crm@shahfiresafety.in';

UPDATE "Users"
SET "PasswordHash" = '$2a$11$T7JTGuBcTiONO1NGPJhuS.2lOyJt6eiGJ.Ibxe7MR8tgtrBghetgm'
WHERE "Email" IN ('field@shahfiresafety.in', 'tech@shahfire.com');
