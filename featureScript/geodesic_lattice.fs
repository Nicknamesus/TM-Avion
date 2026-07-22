FeatureScript 2985;
import(path : "onshape/std/common.fs", version : "2985.0");

annotation { "Feature Type Name" : "Geodesic lattice" }
export const geodesicLattice = defineFeature(function(context is Context, id is Id, definition is map)
precondition 
{
    annotation { "Name" : "Wing solid", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
    definition.wing is Query;

    annotation { "Name" : "Chord plane", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
    definition.plane is Query;

    annotation { "Name" : "Cell size" }
    isLength(definition.cellSize, { (millimeter) : [2, 18, 300] } as LengthBoundSpec);

    annotation { "Name" : "Web thickness" }
    isLength(definition.webThickness, { (millimeter) : [0.2, 0.6, 5] } as LengthBoundSpec);

    annotation { "Name" : "Skin thickness" }
    isLength(definition.skinThickness, { (millimeter) : [0.4, 1.0, 8] } as LengthBoundSpec);    

    annotation { "Name" : "Lightening holes", "Default" : true }
    definition.lightenWebs is boolean;

    annotation { "Name" : "Hole border" }
    isLength(definition.holeBorder, { (millimeter) : [0.3, 1.5, 10] } as LengthBoundSpec);

    annotation { "Name" : "Diagonal half-angle" }
    isAngle(definition.angle, { (degree) : [5, 45, 85] } as AngleBoundSpec);

    annotation { "Name" : "Keep input wing", "Default" : false }
    definition.keepWing is boolean;

    annotation { "Name" : "Close exterior (tip) end", "Default" : true }
    definition.capTip is boolean;

    annotation { "Name" : "Close interior (root) end", "Default" : false }
    definition.capRoot is boolean;

    annotation { "Name" : "Flip tip/root to other span side", "Default" : false }
    definition.flipTip is boolean;
}

