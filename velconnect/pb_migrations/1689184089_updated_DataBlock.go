package migrations

import (
	"encoding/json"

	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
	"github.com/pocketbase/pocketbase/models/schema"
)

func init() {
	m.Register(func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// remove
		collection.Schema.RemoveField("2j8ydmzp")

		// add
		new_owner := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
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
				"displayFields": []
			}
		}`), new_owner)
		collection.Schema.AddField(new_owner)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("3qwwkz4wb0lyi78")
		if err != nil {
			return err
		}

		// add
		del_owner_id := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "2j8ydmzp",
			"name": "owner_id",
			"type": "text",
			"required": false,
			"unique": false,
			"options": {
				"min": null,
				"max": null,
				"pattern": ""
			}
		}`), del_owner_id)
		collection.Schema.AddField(del_owner_id)

		// remove
		collection.Schema.RemoveField("a3d7pkoh")

		return dao.SaveCollection(collection)
	})
}
