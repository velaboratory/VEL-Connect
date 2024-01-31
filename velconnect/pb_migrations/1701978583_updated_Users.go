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

		collection, err := dao.FindCollectionByNameOrId("_pb_users_auth_")
		if err != nil {
			return err
		}

		// update
		edit_user_data := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "xvw8arlm",
			"name": "user_data",
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
		}`), edit_user_data)
		collection.Schema.AddField(edit_user_data)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("_pb_users_auth_")
		if err != nil {
			return err
		}

		// update
		edit_user_data := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
			"system": false,
			"id": "xvw8arlm",
			"name": "user_data",
			"type": "relation",
			"required": false,
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
		}`), edit_user_data)
		collection.Schema.AddField(edit_user_data)

		return dao.SaveCollection(collection)
	})
}
