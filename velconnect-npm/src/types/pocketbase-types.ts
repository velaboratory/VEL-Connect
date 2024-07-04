/**
* This file was @generated using pocketbase-typegen
*/

import type PocketBase from 'pocketbase'
import type { RecordService } from 'pocketbase'

export enum Collections {
	DataBlock = "DataBlock",
	Device = "Device",
	UserCount = "UserCount",
	Users = "Users",
}

// Alias types for improved usability
export type IsoDateString = string
export type RecordIdString = string
export type HTMLString = string

// System fields
export type BaseSystemFields<T = never> = {
	id: RecordIdString
	created: IsoDateString
	updated: IsoDateString
	collectionId: string
	collectionName: Collections
	expand?: T
}

export type AuthSystemFields<T = never> = {
	email: string
	emailVisibility: boolean
	username: string
	verified: boolean
} & BaseSystemFields<T>

// Record types for each collection

export type DataBlockRecord<Tdata = { [key: string]: any }> = {
	block_id?: string
	category?: string
	data?: null | Tdata
	modified_by?: string
	owner?: RecordIdString
}

export type DeviceRecord = {
	current_app?: string
	current_room?: string
	data: RecordIdString
	friendly_name?: string
	friendlier_name?: string
	last_online?: IsoDateString
	modified_by?: string
	os_info?: string
	owner?: RecordIdString
	pairing_code?: string
	past_owners?: RecordIdString[]
}

export type UserCountRecord = {
	app_id?: string
	platform?: string
	room_id?: string
	room_users?: number
	total_users?: number
	version?: string
}

export type UsersRecord = {
	avatar?: string
	devices?: RecordIdString[]
	name?: string
	profiles?: RecordIdString[]
}

// Response types include system fields and match responses from the PocketBase API
export type DataBlockResponse<Tdata = { [key: string]: any }, Texpand = unknown> = Required<DataBlockRecord<Tdata>> & BaseSystemFields<Texpand>
export type DeviceResponse<Texpand = unknown> = Required<DeviceRecord> & BaseSystemFields<Texpand>
export type UserCountResponse<Texpand = unknown> = Required<UserCountRecord> & BaseSystemFields<Texpand>
export type UsersResponse<Texpand = { devices: DeviceResponse[]; profiles: DataBlockResponse[] }> = Required<UsersRecord> & AuthSystemFields<Texpand>

// Types containing all Records and Responses, useful for creating typing helper functions

export type CollectionRecords = {
	DataBlock: DataBlockRecord
	Device: DeviceRecord
	UserCount: UserCountRecord
	Users: UsersRecord
}

export type CollectionResponses = {
	DataBlock: DataBlockResponse
	Device: DeviceResponse
	UserCount: UserCountResponse
	Users: UsersResponse
}

// Type for usage with type asserted PocketBase instance
// https://github.com/pocketbase/js-sdk#specify-typescript-definitions

export type TypedPocketBase = PocketBase & {
	collection(idOrName: 'DataBlock'): RecordService<DataBlockResponse>
	collection(idOrName: 'Device'): RecordService<DeviceResponse>
	collection(idOrName: 'UserCount'): RecordService<UserCountResponse>
	collection(idOrName: 'Users'): RecordService<UsersResponse>
}
