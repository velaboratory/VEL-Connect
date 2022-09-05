CREATE TABLE `APIKey` (
    `key` VARCHAR(64) NOT NULL PRIMARY KEY,
    -- 0 is all access, higher is less
    -- 10 is for headset clients
    `auth_level` INT,
    `date_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `last_used` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Incremented every time this key is used
    `uses` INT DEFAULT 0
);
CREATE TABLE `UserCount` (
    `timestamp` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `hw_id` VARCHAR(64) NOT NULL,
    `app_id` VARCHAR(64) NOT NULL,
    `room_id` VARCHAR(64) NOT NULL,
    `total_users` INT NOT NULL DEFAULT 0,
    `room_users` INT NOT NULL DEFAULT 0,
    `version` VARCHAR(32),
    `platform` VARCHAR(64),
    PRIMARY KEY (`timestamp`, `hw_id`)
);
CREATE TABLE `User` (
    -- user is defined by uuid, to which an email can be added without having to migrate.
    -- then the data that is coming from a user vs device is constant
    -- UUID
    `id` TEXT NOT NULL PRIMARY KEY,
    -- the user's email
    `email` TEXT,
    `username` TEXT,
    -- the first time this device was seen
    `date_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- the last time this device data was modified
    `last_modified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- JSON containing arbitrary data
    `data` TEXT
);
CREATE TABLE `UserDevice` (
    -- the user account's uuid
    `user_id` TEXT NOT NULL,
    -- identifier for the device
    -- This is unique because a device can have only one owner
    `hw_id` TEXT NOT NULL UNIQUE,
    -- when this connection was created
    `date_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`user_id`, `hw_id`)
);
CREATE TABLE `Device` (
    -- Unique identifier for this device
    `hw_id` TEXT NOT NULL PRIMARY KEY,
    -- info about the hardware. Would specify Quest or Windows for example
    `os_info` TEXT,
    -- A human-readable name for this device. Not a username for the game
    `friendly_name` TEXT,
    -- The last source to change this object. Generally this is the device id
    `modified_by` TEXT,
    -- The app_id of the current app. Can be null if app left cleanly
    `current_app` TEXT,
    -- The room_id of the current room. Can be null if room not specified. Could be some other sub-app identifier
    `current_room` TEXT,
    -- changes relatively often. Generated by the headset
    `pairing_code` INT,
    -- the first time this device was seen
    `date_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- the last time this device data was modified
    `last_modified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- JSON containing arbitrary data
    `data` TEXT
);

CREATE TABLE `DataBlock` (
    -- Could be randomly generated. For room data, this is 'appId_roomName'
    `id` TEXT NOT NULL,
    -- id of the owner of this file. Ownership is not transferable because ids may collide,
    -- but the owner could be null for global scope
    `owner_id` TEXT,
    `visibility` ENUM('public', 'private', 'unlisted') NOT NULL DEFAULT 'public',
    -- This is an indexable field to filter out different types of datablocks
    `category` TEXT,
    `date_created` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- The last source to change this object. Generally this is the device id
    `modified_by` TEXT,
    `last_modified` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- the last time this data was fetched individually
    `last_accessed` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- JSON containing arbitrary data
    `data` TEXT,
    PRIMARY KEY (`id`, `owner_id`)
);