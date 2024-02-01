package migrations

import (
	"encoding/json"

	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
	"github.com/pocketbase/pocketbase/models"
)

func init() {
	m.Register(func(db dbx.Builder) error {
		jsonData := `[
			{
				"id": "ve85cwsj7syqvxu",
				"created": "2023-07-06 23:08:13.962Z",
				"updated": "2023-11-03 18:47:06.094Z",
				"name": "UserCount",
				"type": "base",
				"system": false,
				"schema": [
					{
						"system": false,
						"id": "pnhtdbcx",
						"name": "app_id",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "wkf3zyyb",
						"name": "room_id",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "f7k9hdoc",
						"name": "total_users",
						"type": "number",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null
						}
					},
					{
						"system": false,
						"id": "uevek8os",
						"name": "room_users",
						"type": "number",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null
						}
					},
					{
						"system": false,
						"id": "coilxuep",
						"name": "version",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "zee0a2yb",
						"name": "platform",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					}
				],
				"indexes": [],
				"listRule": null,
				"viewRule": null,
				"createRule": "",
				"updateRule": null,
				"deleteRule": null,
				"options": {}
			},
			{
				"id": "fupstz47c55s69f",
				"created": "2023-07-06 23:10:31.321Z",
				"updated": "2023-11-03 18:47:06.120Z",
				"name": "Device",
				"type": "base",
				"system": false,
				"schema": [
					{
						"system": false,
						"id": "1tkrnxqf",
						"name": "os_info",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "knspamfx",
						"name": "friendly_name",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "qfalwg3c",
						"name": "modified_by",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "x0zlup7v",
						"name": "current_app",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "vpzen2th",
						"name": "current_room",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "d0ckgjhm",
						"name": "pairing_code",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "hglbl7da",
						"name": "last_online",
						"type": "date",
						"required": false,
						"unique": false,
						"options": {
							"min": "",
							"max": ""
						}
					},
					{
						"system": false,
						"id": "qxsvm1rf",
						"name": "data",
						"type": "relation",
						"required": true,
						"unique": false,
						"options": {
							"collectionId": "3qwwkz4wb0lyi78",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": 1,
							"displayFields": [
								"data"
							]
						}
					},
					{
						"system": false,
						"id": "nfernq2q",
						"name": "owner",
						"type": "relation",
						"required": false,
						"unique": false,
						"options": {
							"collectionId": "_pb_users_auth_",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": 1,
							"displayFields": [
								"username"
							]
						}
					},
					{
						"system": false,
						"id": "p1aruqz5",
						"name": "past_owners",
						"type": "relation",
						"required": false,
						"unique": false,
						"options": {
							"collectionId": "_pb_users_auth_",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": null,
							"displayFields": [
								"username"
							]
						}
					}
				],
				"indexes": [],
				"listRule": "",
				"viewRule": "",
				"createRule": null,
				"updateRule": "",
				"deleteRule": null,
				"options": {}
			},
			{
				"id": "3qwwkz4wb0lyi78",
				"created": "2023-07-06 23:12:11.113Z",
				"updated": "2024-01-31 21:26:53.835Z",
				"name": "DataBlock",
				"type": "base",
				"system": false,
				"schema": [
					{
						"system": false,
						"id": "wbifl8pv",
						"name": "category",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "5a3nwg7m",
						"name": "modified_by",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "mkzyfsng",
						"name": "data",
						"type": "json",
						"required": false,
						"unique": false,
						"options": {}
					},
					{
						"system": false,
						"id": "a3d7pkoh",
						"name": "owner",
						"type": "relation",
						"required": false,
						"unique": false,
						"options": {
							"collectionId": "_pb_users_auth_",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": 1,
							"displayFields": [
								"username"
							]
						}
					},
					{
						"system": false,
						"id": "80tmi6fm",
						"name": "block_id",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					}
				],
				"indexes": [
					"CREATE INDEX ` + "`" + `idx_aYVfg1q` + "`" + ` ON ` + "`" + `DataBlock` + "`" + ` (` + "`" + `block_id` + "`" + `)"
				],
				"listRule": "",
				"viewRule": "",
				"createRule": "",
				"updateRule": "",
				"deleteRule": null,
				"options": {}
			},
			{
				"id": "_pb_users_auth_",
				"created": "2023-11-03 18:47:06.067Z",
				"updated": "2024-01-31 21:26:53.820Z",
				"name": "Users",
				"type": "auth",
				"system": false,
				"schema": [
					{
						"system": false,
						"id": "users_name",
						"name": "name",
						"type": "text",
						"required": false,
						"unique": false,
						"options": {
							"min": null,
							"max": null,
							"pattern": ""
						}
					},
					{
						"system": false,
						"id": "users_avatar",
						"name": "avatar",
						"type": "file",
						"required": false,
						"unique": false,
						"options": {
							"maxSelect": 1,
							"maxSize": 5242880,
							"mimeTypes": [
								"image/jpeg",
								"image/png",
								"image/svg+xml",
								"image/gif",
								"image/webp"
							],
							"thumbs": null,
							"protected": false
						}
					},
					{
						"system": false,
						"id": "1hwaooub",
						"name": "devices",
						"type": "relation",
						"required": false,
						"unique": false,
						"options": {
							"collectionId": "fupstz47c55s69f",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": null,
							"displayFields": []
						}
					},
					{
						"system": false,
						"id": "xvw8arlm",
						"name": "profiles",
						"type": "relation",
						"required": false,
						"unique": false,
						"options": {
							"collectionId": "3qwwkz4wb0lyi78",
							"cascadeDelete": false,
							"minSelect": null,
							"maxSelect": null,
							"displayFields": [
								"data"
							]
						}
					}
				],
				"indexes": [],
				"listRule": "id = @request.auth.id",
				"viewRule": "id = @request.auth.id",
				"createRule": "",
				"updateRule": "id = @request.auth.id",
				"deleteRule": "id = @request.auth.id",
				"options": {
					"allowEmailAuth": true,
					"allowOAuth2Auth": true,
					"allowUsernameAuth": true,
					"exceptEmailDomains": null,
					"manageRule": null,
					"minPasswordLength": 6,
					"onlyEmailDomains": null,
					"requireEmail": false
				}
			}
		]`

		collections := []*models.Collection{}
		if err := json.Unmarshal([]byte(jsonData), &collections); err != nil {
			return err
		}

		return daos.New(db).ImportCollections(collections, true, nil)
	}, func(db dbx.Builder) error {
		return nil
	})
}
