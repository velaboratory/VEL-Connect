DROP TABLE IF EXISTS `Room`;
CREATE TABLE `Room` (
    `room_id` VARCHAR(64) NOT NULL PRIMARY KEY,
    `date_created` TIMESTAMP DEFAULT CURRENT_TIME,
    `last_modified` TIMESTAMP DEFAULT CURRENT_TIME,
    -- Can be null if no owner
    `owner` VARCHAR(64),
    -- array of hw_ids of users allowed. Always includes the owner. Null for public
    `whitelist` JSON,
    CHECK (JSON_VALID(`whitelist`)),
    `tv_url` VARCHAR(1024),
    `carpet_color` VARCHAR(8),
    `room_details` JSON,
    CHECK (JSON_VALID(`room_details`))
);
DROP TABLE IF EXISTS `Headset`;
CREATE TABLE `Headset` (
    `hw_id` VARCHAR(64) NOT NULL PRIMARY KEY,
    -- The room_id of the owned room
    `owned_room` VARCHAR(64),
    -- The room_id of the current room. Can be null if room not specified
    `current_room` VARCHAR(64),
    -- changes relatively often. Generated by the headset
    `pairing_code` INT,
    `date_created` TIMESTAMP DEFAULT CURRENT_TIME,
    -- the last time this headset was actually seen
    `last_used` TIMESTAMP DEFAULT CURRENT_TIME,
    `user_color` VARCHAR(8),
    `user_name` VARCHAR(64),
    -- Stuff like player color, nickname, whiteboard state
    `user_details` JSON,
    CHECK (JSON_VALID(`user_details`))
);
DROP TABLE IF EXISTS `APIKey`;
CREATE TABLE `APIKey` (
    `key` VARCHAR(64) NOT NULL PRIMARY KEY,
    -- 0 is all access, higher is less
    -- 10 is for headset clients
    `auth_level` INT,
    `date_created` TIMESTAMP DEFAULT CURRENT_TIME,
    `last_used` TIMESTAMP DEFAULT CURRENT_TIME,
    `uses` INT DEFAULT 0
);