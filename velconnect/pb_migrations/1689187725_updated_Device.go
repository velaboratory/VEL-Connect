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

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		// update
		edit_owner := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
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
		}`), edit_owner)
		collection.Schema.AddField(edit_owner)

		// update
		edit_past_owners := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
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
		}`), edit_past_owners)
		collection.Schema.AddField(edit_past_owners)

		return dao.SaveCollection(collection)
	}, func(db dbx.Builder) error {
		dao := daos.New(db);

		collection, err := dao.FindCollectionByNameOrId("fupstz47c55s69f")
		if err != nil {
			return err
		}

		// update
		edit_owner := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
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
				"displayFields": []
			}
		}`), edit_owner)
		collection.Schema.AddField(edit_owner)

		// update
		edit_past_owners := &schema.SchemaField{}
		json.Unmarshal([]byte(`{
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
				"displayFields": []
			}
		}`), edit_past_owners)
		collection.Schema.AddField(edit_past_owners)

		return dao.SaveCollection(collection)
	})
}
